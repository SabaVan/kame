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
    // This class is used for testing to avoid dynamic
    public class PlaylistEvent
    {
        public string Action { get; set; } = "";
    }

    public class BarStateUpdaterService : BackgroundService
    {
        private readonly ILogger<BarStateUpdaterService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<BarHub> _barHub;

        // Thread-safe cache of active bars
        // Key: Bar ID
        // Value: Bar object (updated when bar state changes or cache is refreshed)
        // Benefits: Avoids repeated database queries and enables safe concurrent iteration
        private static readonly ConcurrentDictionary<Guid, Bar> ActiveBarsCache = new();

        private readonly TimeSpan _barUpdateInterval = TimeSpan.FromMinutes(5);
        private readonly TimeSpan _playlistUpdateInterval = TimeSpan.FromMinutes(1);

        public BarStateUpdaterService(
            ILogger<BarStateUpdaterService> logger,
            IServiceScopeFactory scopeFactory,
            IHubContext<BarHub> barHub)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _barHub = barHub;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _ = RunBarStateUpdater(stoppingToken);
            _ = RunPlaylistUpdater(stoppingToken);
            return Task.CompletedTask;
        }

        private async Task RunBarStateUpdater(CancellationToken stoppingToken)
        {
            _logger.LogInformation("BarStateUpdater loop started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var barService = scope.ServiceProvider.GetRequiredService<IBarService>();

                    // Update cache with current bar states from database
                    await RefreshActiveBarsCache(barService, stoppingToken);

                    // Check schedules and update states
                    await barService.CheckSchedule(DateTime.UtcNow);

                    _logger.LogInformation("Bar states updated at {Time}. Active bars in cache: {Count}", 
                        DateTime.UtcNow, ActiveBarsCache.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating bar states");
                }

                await Task.Delay(_barUpdateInterval, stoppingToken);
            }
        }

        /// <summary>
        /// Refreshes the ActiveBarsCache with current bars from the database.
        /// Thread-safe: Uses ConcurrentDictionary.AddOrUpdate for atomic operations.
        /// </summary>
        private async Task RefreshActiveBarsCache(IBarService barService, CancellationToken cancellationToken)
        {
            try
            {
                var activeBars = await barService.GetActiveBars();

                // Clear old entries that are no longer active
                var activeBarIds = new HashSet<Guid>(activeBars.Select(b => b.Id));
                foreach (var existingId in ActiveBarsCache.Keys.ToList())
                {
                    if (!activeBarIds.Contains(existingId))
                    {
                        ActiveBarsCache.TryRemove(existingId, out _);
                        _logger.LogDebug("Removed bar {BarId} from cache", existingId);
                    }
                }

                // Add or update bars in cache
                foreach (var bar in activeBars)
                {
                    ActiveBarsCache.AddOrUpdate(
                        bar.Id,
                        bar, // Add if new
                        (key, existingBar) => bar // Update with latest state
                    );
                }

                _logger.LogDebug("Cache refreshed with {Count} active bars", activeBars.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing active bars cache");
            }
        }

        private async Task RunPlaylistUpdater(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PlaylistUpdater loop started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var playlistRepo = scope.ServiceProvider.GetRequiredService<IPlaylistRepository>();
                    var playlistService = scope.ServiceProvider.GetRequiredService<IPlaylistService>();

                    // Use cached active bars for safe concurrent iteration
                    // No database query needed - cache is updated by RunBarStateUpdater
                    var activeBars = ActiveBarsCache.Values.ToList();

                    if (activeBars.Count == 0)
                    {
                        _logger.LogDebug("No active bars in cache");
                        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                        continue;
                    }

                    foreach (var bar in activeBars)
                    {
                        var playlist = await playlistRepo.GetByIdAsync(bar.CurrentPlaylistId);
                        if (playlist == null)
                        {
                            _logger.LogWarning(
                                "Playlist {PlaylistId} not found for bar {BarId}",
                                bar.CurrentPlaylistId, bar.Id
                            );
                            continue;
                        }

                        var nextSong = playlist.GetNextSong();
                        if (nextSong == null)
                        {
                            _logger.LogInformation(
                                "No songs left in playlist {PlaylistId} for bar {BarId}",
                                playlist.Id, bar.Id
                            );
                            continue;
                        }

                        var duration = nextSong.Duration ?? TimeSpan.FromSeconds(15); // 15 sec if duration is missing
                        if (duration <= TimeSpan.Zero)
                            duration = TimeSpan.FromSeconds(1);

                        _logger.LogInformation(
                            "Bar {BarId}: playing song '{Title}' ({Duration}s)",
                            bar.Id, nextSong.Title, duration.TotalSeconds
                        );

                        await _barHub.Clients.Group(bar.Id.ToString()).SendAsync(
                            "PlaylistUpdated",
                            new PlaylistEvent { Action = "song_started" }, // test-friendly payload
                            new
                            {
                                playlistId = playlist.Id,
                                songId = nextSong.Id,
                                songTitle = nextSong.Title,
                                duration = nextSong.Duration.GetValueOrDefault(),
                                action = "song_started"
                            },
                            stoppingToken
                        );

                        await Task.Delay(duration, stoppingToken);

                        playlist.RemoveSong(nextSong.Id);
                        
                        // Reorder and persist position changes to database
                        playlist.ReorderByBids();
                        foreach (var song in playlist.Songs)
                        {
                            await playlistRepo.UpdatePlaylistSongAsync(song);
                        }
                        
                        await playlistRepo.UpdateAsync(playlist);

                        await _barHub.Clients.Group(bar.Id.ToString()).SendAsync(
                            "PlaylistUpdated",
                            new PlaylistEvent { Action = "song_ended" }, // test-friendly payload
                            new
                            {
                                playlistId = playlist.Id,
                                songId = nextSong.Id,
                                songTitle = nextSong.Title,
                                duration = nextSong.Duration.GetValueOrDefault(),
                                action = "song_ended"
                            },
                            stoppingToken
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating playlists");
                }

                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }

        }

        /// <summary>
        /// Invalidates a specific bar from the cache when its state changes.
        /// Useful when bar state is updated outside the background service.
        /// </summary>
        public static void InvalidateBarCache(Guid barId)
        {
            ActiveBarsCache.TryRemove(barId, out _);
        }

        /// <summary>
        /// Clears the entire active bars cache.
        /// Useful for forcing a complete refresh on next update cycle.
        /// </summary>
        public static void ClearCache()
        {
            ActiveBarsCache.Clear();
        }

        /// <summary>
        /// Gets the current count of cached active bars.
        /// Useful for monitoring and debugging.
        /// </summary>
        public static int GetCacheCount()
        {
            return ActiveBarsCache.Count;
        }

        /// <summary>
        /// Gets all cached bar IDs.
        /// Useful for monitoring which bars are cached.
        /// </summary>
        public static IEnumerable<Guid> GetCachedBarIds()
        {
            return ActiveBarsCache.Keys;
        }

    }
}