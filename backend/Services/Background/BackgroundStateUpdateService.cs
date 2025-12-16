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

                var nextSong = playlist.GetNextSong();
                if (nextSong == null) return;

                var duration = nextSong.Duration ?? TimeSpan.FromSeconds(15);
                if (duration <= TimeSpan.Zero) duration = TimeSpan.FromSeconds(1);

                _logger.LogInformation("Bar {BarId} playing: {Title}", bar.Id, nextSong.Title);

                // 1. Notify Clients: Start
                await _barHub.Clients.Group(bar.Id.ToString()).SendAsync(
                    "PlaylistUpdated",
                    new PlaylistEvent { Action = "song_started" },
                    new 
                    { 
                        playlistId = playlist.Id, 
                        songId = nextSong.Id, 
                        songTitle = nextSong.Title, 
                        duration = duration.TotalSeconds,
                        action = "song_started"
                    },
                    stoppingToken
                );

                // 2. IMMEDIATE DB UPDATE (Prevents several-minute startup delay)
                playlist.RemoveSong(nextSong.Id);
                playlist.ReorderByBids();
                foreach (var song in playlist.Songs) await playlistRepo.UpdatePlaylistSongAsync(song);
                await playlistRepo.UpdateAsync(playlist);

                // 3. Wait for song duration
                await Task.Delay(duration, stoppingToken);

                // 4. Notify Clients: End
                await _barHub.Clients.Group(bar.Id.ToString()).SendAsync(
                    "PlaylistUpdated",
                    new PlaylistEvent { Action = "song_ended" },
                    new 
                    { 
                        playlistId = playlist.Id, 
                        songId = nextSong.Id,
                        action = "song_ended" 
                    },
                    stoppingToken
                );
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