using backend.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace backend.Services.Background
{
    public class BarStateUpdaterService : BackgroundService
    {
        private readonly ILogger<BarStateUpdaterService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        private readonly TimeSpan _barUpdateInterval = TimeSpan.FromMinutes(5);
        private readonly TimeSpan _playlistUpdateInterval = TimeSpan.FromMinutes(1);

        public BarStateUpdaterService(ILogger<BarStateUpdaterService> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
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

                    // to be implemented

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating playlists");
                }

                await Task.Delay(_playlistUpdateInterval, stoppingToken);
            }
        }
    }
}
