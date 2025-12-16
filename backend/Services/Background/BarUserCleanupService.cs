using backend.Models;
using backend.Repositories.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace backend.Services.Background
{
    /// <summary>
    /// Periodically removes BarUserEntry records that haven't been updated
    /// (EnteredAt) within the configured inactivity threshold.
    /// </summary>
    public class BarUserCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BarUserCleanupService> _logger;

        // How often to run the cleanup loop
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);
        // Consider a user inactive if no activity for this duration
        private readonly TimeSpan _inactivityThreshold = TimeSpan.FromMinutes(30);

        public BarUserCleanupService(IServiceScopeFactory scopeFactory, ILogger<BarUserCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("BarUserCleanupService started. Cleanup every {Interval} minutes. Inactivity threshold: {Threshold} minutes.",
                _cleanupInterval.TotalMinutes, _inactivityThreshold.TotalMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var barUserRepo = scope.ServiceProvider.GetRequiredService<IBarUserEntryRepository>();

                    var cutoff = DateTime.UtcNow - _inactivityThreshold;
                    var staleEntries = await barUserRepo.GetEntriesOlderThanAsync(cutoff);

                    if (staleEntries != null && staleEntries.Count > 0)
                    {
                        _logger.LogInformation("Found {Count} stale BarUserEntry records to remove (cutoff: {Cutoff}).", staleEntries.Count, cutoff);

                        foreach (var entry in staleEntries)
                        {
                            try
                            {
                                await barUserRepo.RemoveEntryAsync(entry.BarId, entry.UserId);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error removing stale BarUserEntry {BarId}/{UserId}", entry.BarId, entry.UserId);
                            }
                        }

                        await barUserRepo.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while cleaning up BarUserEntry records");
                }

                await Task.Delay(_cleanupInterval, stoppingToken);
            }
        }
    }
}
