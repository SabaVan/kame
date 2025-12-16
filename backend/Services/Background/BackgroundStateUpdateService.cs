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

        private readonly TimeSpan _barUpdateInterval = TimeSpan.FromMinutes(1); // Check schedule every minute

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

                    // FIX: Prime the cache immediately so PlaylistUpdater doesn't start with an empty list
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
                    
                                    await barService.CheckSchedule(DateTime.UtcNow);
                                    await RefreshActiveBarsCache(barService, stoppingToken);
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
            // Tracks which bars are currently running a playlist task to prevent duplicates
            var runningTasks = new ConcurrentDictionary<Guid, Task>();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var activeBars = ActiveBarsCache.Values.ToList();

                    foreach (var bar in activeBars)
                    {
                        // Only start a task for this bar if one isn't already running
                        if (!runningTasks.TryGetValue(bar.Id, out var existingTask) || existingTask.IsCompleted)
                        {
                            runningTasks[bar.Id] = Task.Run(() => ProcessBarPlaylistAsync(bar, stoppingToken), stoppingToken);
                        }
                    }

                    // Clean up completed tasks from the dictionary to save memory
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
                if (playlist == null) return;

                var nextSong = playlist.GetNextSong();
                if (nextSong == null) return;

                var duration = nextSong.Duration ?? TimeSpan.FromSeconds(15);
                if (duration <= TimeSpan.Zero) duration = TimeSpan.FromSeconds(1);

                _logger.LogInformation("Bar {BarId} playing: {Title}", bar.Id, nextSong.Title);

                // Start Song SignalR
                await _barHub.Clients.Group(bar.Id.ToString()).SendAsync(
                    "PlaylistUpdated",
                    new PlaylistEvent { Action = "song_started" },
                    new { playlistId = playlist.Id, songId = nextSong.Id, songTitle = nextSong.Title, duration = duration.TotalSeconds },
                    stoppingToken
                );

                // Wait for the song duration
                await Task.Delay(duration, stoppingToken);

                // Update Database
                playlist.RemoveSong(nextSong.Id);
                playlist.ReorderByBids();
                foreach (var song in playlist.Songs) await playlistRepo.UpdatePlaylistSongAsync(song);
                await playlistRepo.UpdateAsync(playlist);

                // End Song SignalR
                await _barHub.Clients.Group(bar.Id.ToString()).SendAsync(
                    "PlaylistUpdated",
                    new PlaylistEvent { Action = "song_ended" },
                    new { playlistId = playlist.Id, action = "song_ended" },
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

            // Remove bars no longer open
            foreach (var existingId in ActiveBarsCache.Keys.ToList())
            {
                if (!activeBarIds.Contains(existingId)) ActiveBarsCache.TryRemove(existingId, out _);
            }

            // Add/Update current bars
            foreach (var bar in activeBars)
            {
                ActiveBarsCache.AddOrUpdate(bar.Id, bar, (key, existing) => bar);
            }
        }

        // --- Cache Helpers ---
        public static void InvalidateBarCache(Guid barId) => ActiveBarsCache.TryRemove(barId, out _);
        public static void ClearCache() => ActiveBarsCache.Clear();
    }
}