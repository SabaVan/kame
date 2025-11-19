using backend.Hubs;
using backend.Models;
using backend.Repositories.Interfaces;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace backend.Services.Background
{
    public class BarStateUpdaterService : BackgroundService
    {
        private readonly ILogger<BarStateUpdaterService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<BarHub> _barHub;

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
            // Run independent loops
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

                    await barService.CheckSchedule(DateTime.UtcNow);
                    _logger.LogInformation("Bar states updated at {Time}", DateTime.UtcNow);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating bar states");
                }

                await Task.Delay(_barUpdateInterval, stoppingToken);
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
                    var barService = scope.ServiceProvider.GetRequiredService<IBarService>();
                    var playlistRepo = scope.ServiceProvider.GetRequiredService<IPlaylistRepository>();
                    var playlistService = scope.ServiceProvider.GetRequiredService<IPlaylistService>();

                    var activeBars = await barService.GetActiveBars();

                    foreach (var bar in activeBars)
                    {
                        var playlist = await playlistRepo.GetByIdAsync(bar.CurrentPlaylistId);
                        if (playlist == null)
                        {
                            _logger.LogWarning("Playlist {PlaylistId} not found for bar {BarId}", bar.CurrentPlaylistId, bar.Id);
                            continue;
                        }

                        var nextSong = playlist.GetNextSong();
                        if (nextSong == null)
                        {
                            _logger.LogInformation("No songs left in playlist {PlaylistId} for bar {BarId}", playlist.Id, bar.Id);
                            continue;
                        }

                        // Determine duration (fallback 1 sec if missing)
                        var duration = nextSong.Duration.GetValueOrDefault(TimeSpan.FromSeconds(1));
                        if (duration <= TimeSpan.Zero)
                            duration = TimeSpan.FromSeconds(1);

                        // For testing you can temporarily override duration
                        duration = TimeSpan.FromSeconds(15);

                        _logger.LogInformation(
                            "Bar {BarId}: playing song '{Title}' ({Duration}s)",
                            bar.Id, nextSong.Title, duration.TotalSeconds
                        );

                        // Notify frontend that song started
                        await _barHub.Clients.Group(bar.Id.ToString()).SendAsync("PlaylistUpdated", new
                        {
                            playlistId = playlist.Id,
                            songId = nextSong.Id,
                            songTitle = nextSong.Title,
                            duration = nextSong.Duration.GetValueOrDefault(),
                            action = "song_started"
                        });

                        // Wait for song duration or until cancelled
                        try
                        {
                            await Task.Delay(duration, stoppingToken);
                        }
                        catch (TaskCanceledException)
                        {
                            // If stoppingToken triggered, exit gracefully
                            return;
                        }

                        // Remove song after playing
                        playlist.RemoveSong(nextSong.Id);

                        // Reorder playlist after changes and save positions in DB
                        await playlistService.ReorderAndSavePlaylistAsync(playlist.Id);

                        // Optional: Update the playlist entity in case repo requires it
                        await playlistRepo.UpdateAsync(playlist);

                        // Notify frontend that song ended
                        await _barHub.Clients.Group(bar.Id.ToString()).SendAsync("PlaylistUpdated", new
                        {
                            playlistId = playlist.Id,
                            songId = nextSong.Id,
                            songTitle = nextSong.Title,
                            duration = nextSong.Duration.GetValueOrDefault(),
                            action = "song_ended"
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating playlists");
                }

                // Short delay between cycles to avoid tight loop
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    return;
                }
            }

        }

    }
}