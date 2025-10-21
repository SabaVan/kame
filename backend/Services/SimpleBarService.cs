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
        public async Task<Result<Bar?>> SetBarState(Guid BarId, BarState newState)
        {
            Bar? bar = await _bars.GetByIdAsync(BarId);
            if (bar == null)
                return Result<Bar?>.Failure(StandardErrors.NotFound);
            bar.SetState(newState);
            await _bars.SaveChangesAsync();
            return Result<Bar?>.Success(bar);
        }
        public async Task<Result<BarUserEntry>> EnterBar(Bar bar, User user)
        {
            BarUserEntry entry = new BarUserEntry(bar, user);
            await _barUserEntries.AddEntryAsync(entry);
            await _barUserEntries.SaveChangesAsync();
            return Result<BarUserEntry>.Success(entry);
        }
        public async Task<Result<BarUserEntry>> LeaveBar(Bar bar, User user)
        {
            var entryResult = await _barUserEntries.FindEntryAsync(bar.Id, user.Id);
            if (entryResult.IsFailure)
                return Result<BarUserEntry>.Failure(StandardErrors.NonexistentEntity);
            var entry = entryResult.Value;
            await _barUserEntries.RemoveEntryAsync(entry!);
            await _barUserEntries.SaveChangesAsync();
            return entryResult;
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
