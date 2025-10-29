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

                    var activeBars = await barService.GetActiveBars();

                    foreach (var bar in activeBars)
                    {
                        var playlist = await playlistRepo.GetByIdAsync(bar.CurrentPlaylistId);
                        if (playlist == null)
                        {
                            _logger.LogWarning("Playlist {PlaylistId} not found for bar {BarId}", bar.CurrentPlaylistId, bar.Id);
                            continue;
                        }

                        var song = playlist.GetNextSong();
                        if (song == null)
                        {
                            _logger.LogInformation("No songs left in playlist {PlaylistId} for bar {BarId}", playlist.Id, bar.Id);
                            continue;
                        }

                        var duration = song.Duration.GetValueOrDefault(TimeSpan.FromSeconds(1));
                        if (duration <= TimeSpan.Zero)
                            duration = TimeSpan.FromSeconds(1);

                        // make song 15 sec for testing
                        duration = TimeSpan.FromSeconds(15);

                        _logger.LogInformation(
                            "Bar {BarId}: playing song '{Title}' ({Duration}s)",
                            bar.Id, song.Title, duration.TotalSeconds
                        );


                        // Notify frontend that song started
                        await _barHub.Clients.Group(bar.Id.ToString()).SendAsync("PlaylistUpdated", new
                        {
                            playlistId = playlist.Id,
                            songId = song.Id,
                            songTitle = song.Title,
                            duration = song.Duration.GetValueOrDefault(),
                            action = "song_started"
                        });



                        // Wait for duration
                        await Task.Delay(duration, stoppingToken);

                        // Remove song after playing
                        playlist.RemoveSong(song.Id);
                        await playlistRepo.UpdateAsync(playlist);

                        // Notify frontend that song finished
                        await _barHub.Clients.Group(bar.Id.ToString()).SendAsync("PlaylistUpdated", new
                        {
                            playlistId = playlist.Id,
                            songId = song.Id,
                            songTitle = song.Title,
                            duration = song.Duration.GetValueOrDefault(),
                            action = "song_ended"
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating playlists");
                }

                // Wait between cycles
                //await Task.Delay(_playlistUpdateInterval, stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
    }
}
