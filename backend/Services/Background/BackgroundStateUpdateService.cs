using backend.Hubs;
using backend.Models;
using backend.Repositories.Interfaces;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace backend.Services.Background
{
    public class PlaylistEvent
    {
        public string Action { get; set; } = "";
    }

    public class BarStateUpdaterService : BackgroundService
    {
        private readonly ILogger<BarStateUpdaterService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<BarHub> _barHub;

        // Cache to track which bars are currently "Open"
        private static readonly ConcurrentDictionary<Guid, Bar> ActiveBarsCache = new();

        private readonly TimeSpan _barUpdateInterval = TimeSpan.FromMinutes(1);

        public BarStateUpdaterService(
            ILogger<BarStateUpdaterService> logger,
            IServiceScopeFactory scopeFactory,
            IHubContext<BarHub> barHub)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _barHub = barHub;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("BarStateUpdaterService starting...");

            // Prime the cache immediately on startup
            using (var scope = _scopeFactory.CreateScope())
            {
                var barService = scope.ServiceProvider.GetRequiredService<IBarService>();
                await barService.CheckSchedule(DateTime.UtcNow);
                await RefreshActiveBarsCache(barService, stoppingToken);
            }

            // Run both loops in parallel
            var barStateTask = RunBarStateUpdater(stoppingToken);
            var playlistTask = RunPlaylistUpdater(stoppingToken);

            await Task.WhenAll(barStateTask, playlistTask);
        }

        private async Task RunBarStateUpdater(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var barService = scope.ServiceProvider.GetRequiredService<IBarService>();

                    // 1. Update DB states based on current time
                    await barService.CheckSchedule(DateTime.UtcNow);

                    // 2. Sync memory cache with DB
                    await RefreshActiveBarsCache(barService, stoppingToken);

                    _logger.LogDebug("Bar states synchronized. Active bars: {Count}", ActiveBarsCache.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in BarStateUpdater loop");
                }
                await Task.Delay(_barUpdateInterval, stoppingToken);
            }
        }

        private async Task RunPlaylistUpdater(CancellationToken stoppingToken)
        {
            var runningTasks = new ConcurrentDictionary<Guid, Task>();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var activeBars = ActiveBarsCache.Values.ToList();

                    foreach (var bar in activeBars)
                    {
                        // Prevent duplicate tasks for the same bar
                        if (!runningTasks.TryGetValue(bar.Id, out var existingTask) || existingTask.IsCompleted)
                        {
                            runningTasks[bar.Id] = Task.Run(() => ProcessBarPlaylistAsync(bar, stoppingToken), stoppingToken);
                        }
                    }

                    // Memory Cleanup: Remove completed task references
                    foreach (var key in runningTasks.Keys.ToList())
                    {
                        if (runningTasks[key].IsCompleted) runningTasks.TryRemove(key, out _);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in global PlaylistUpdater loop");
                }

                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }

        private async Task ProcessBarPlaylistAsync(Bar bar, CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var playlistRepo = scope.ServiceProvider.GetRequiredService<IPlaylistRepository>();

                var playlist = await playlistRepo.GetByIdAsync(bar.CurrentPlaylistId);
                if (playlist == null || !playlist.Songs.Any()) return;

                var nextSongEntry = playlist.Songs
                    .OrderByDescending(s => s.CurrentBid)
                    .ThenBy(s => s.AddedAt)
                    .FirstOrDefault();

                if (nextSongEntry == null) return;

                var duration = TimeSpan.FromSeconds(15);

                await _barHub.Clients.Group(bar.Id.ToString()).SendAsync(
                    "PlaylistUpdated",
                    new
                    {
                        action = "song_started",
                        playlistId = playlist.Id,
                        songId = nextSongEntry.SongId,
                        songTitle = nextSongEntry.Song.Title,
                        duration = duration.TotalSeconds
                    },
                    stoppingToken
                );

                await Task.Delay(duration, stoppingToken);

                var freshPlaylist = await playlistRepo.GetByIdAsync(bar.CurrentPlaylistId);
                if (freshPlaylist != null)
                {
                    freshPlaylist.RemoveSong(nextSongEntry.SongId);
                    freshPlaylist.ReorderByBids();
                    await playlistRepo.UpdateAsync(freshPlaylist);
                }

                await _barHub.Clients.Group(bar.Id.ToString()).SendAsync(
                    "PlaylistUpdated",
                    new { action = "song_ended", playlistId = bar.CurrentPlaylistId },
                    stoppingToken
                );
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing playlist for Bar {BarId}", bar.Id);
            }
        }

        private async Task RefreshActiveBarsCache(IBarService barService, CancellationToken stoppingToken)
        {
            var activeBars = await barService.GetActiveBars();
            var activeBarIds = new HashSet<Guid>(activeBars.Select(b => b.Id));

            // Cleanup stale bars
            foreach (var existingId in ActiveBarsCache.Keys.ToList())
            {
                if (!activeBarIds.Contains(existingId)) ActiveBarsCache.TryRemove(existingId, out _);
            }

            // Upsert current bars
            foreach (var bar in activeBars)
            {
                ActiveBarsCache.AddOrUpdate(bar.Id, bar, (key, existing) => bar);
            }
        }

        public static void InvalidateBarCache(Guid barId) => ActiveBarsCache.TryRemove(barId, out _);
        public static void ClearCache() => ActiveBarsCache.Clear();
    }
}