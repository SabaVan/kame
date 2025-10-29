using backend.Models;
using backend.Repositories.Interfaces;
using backend.Utils.Errors;
using backend.Services.Interfaces;
using backend.Common;
using backend.Shared.Enums;
namespace backend.Services
{
    public class SimpleBarService : IBarService
    {
        private readonly IBarRepository _bars;
        private readonly IBarUserEntryRepository _barUserEntries;
        // private readonly IUserRepository _users;
        // private readonly IPlaylistRepository _playlistService;
        private readonly ICreditService _creditService;
        public SimpleBarService(IBarRepository bars, IBarUserEntryRepository barUserEntries, ICreditService creditService)
        {
            ArgumentNullException.ThrowIfNull(bars);
            ArgumentNullException.ThrowIfNull(barUserEntries);
            //ArgumentNullException.ThrowIfNull(users);
            //ArgumentNullException.ThrowIfNull(playlistService);
            ArgumentNullException.ThrowIfNull(creditService);

            _bars = bars;
            _barUserEntries = barUserEntries;
            //_users = users;
            //_playlistService = playlistService;
            _creditService = creditService;
        }
        public async Task<Bar?> GetDefaultBar()
        {
            return (await _bars.GetAllAsync()).FirstOrDefault();
        }
        public async Task<List<Bar>> GetActiveBars()
        {
            List <Bar> activeBars = new();
            var activeBarsIds = await _barUserEntries.GetAllUniqueBarIdsAsync();
            foreach (Guid barId in activeBarsIds)
            {
                var bar = await _bars.GetByIdAsync(barId);
                if (bar != null)
                    activeBars.Add(bar);
            }
            return activeBars;
        }
        public async Task<Result<Bar?>> SetBarState(Guid BarId, BarState newState)
        {
            Bar? bar = await _bars.GetByIdAsync(BarId);
            if (bar == null)
                return Result<Bar?>.Failure(StandardErrors.NotFound);
            bar.SetState(newState);
            await _bars.SaveChangesAsync();
            return Result<Bar?>.Success(bar);
        }
        /// <summary>
        /// Creates a BarUserEntry in the database.  
        /// Returns failure if:
        /// - The bar does not exist,
        /// - The bar is not open,
        /// - The entry already exists.
        /// </summary>
        public async Task<Result<BarUserEntry>> EnterBar(Guid barId, Guid userId)
        {
            // Validate bar existence
            var bar = await _bars.GetByIdAsync(barId);
            if (bar == null)
                return Result<BarUserEntry>.Failure(StandardErrors.NonexistentBar);

            // Validate bar state
            if (bar.State != BarState.Open)
                return Result<BarUserEntry>.Failure(StandardErrors.InvalidBarAction);

            // Try add entry (repository handles duplicate detection)
            var result = await _barUserEntries.AddEntryAsync(barId, userId);
            if (result.IsFailure)
                return result; // Pass through repository error (e.g. duplicate entry)

            // Commit if add was successful
            await _barUserEntries.SaveChangesAsync();

            return result; // Already a success result from repository
        }

        public async Task<Result<BarUserEntry>> LeaveBar(Guid barId, Guid userId)
        {
            // Validate bar existence
            var bar = await _bars.GetByIdAsync(barId);
            if (bar == null)
                return Result<BarUserEntry>.Failure(StandardErrors.NonexistentBar);

            // Validate bar state
            if (bar.State != BarState.Open)
                return Result<BarUserEntry>.Failure(StandardErrors.InvalidBarAction);

            // Try add entry (repository handles duplicate detection)
            var result = await _barUserEntries.RemoveEntryAsync(barId, userId);
            if (result.IsFailure)
                return result; // StandardErrors.NonexistentEntry

            // Commit if add was successful
            await _barUserEntries.SaveChangesAsync();

            return result; // Already a success result from repository
        }
        public async Task CheckSchedule(DateTime nowUtc)
        {
            bool BarStateWasChanged = false;
            var allBars = await _bars.GetAllAsync();
            foreach (var bar in allBars)
            {
                if (bar.ShouldBeOpen(nowUtc))
                {
                    if (bar.State == BarState.Closed)
                    {
                        bar.SetState(BarState.Open);
                        BarStateWasChanged = true;
                    }
                }
                else if (bar.State == BarState.Open)
                {
                    bar.SetState(BarState.Closed);
                    BarStateWasChanged = true;
                }
            }
            if (BarStateWasChanged) await _bars.SaveChangesAsync();
        }
    }
}
