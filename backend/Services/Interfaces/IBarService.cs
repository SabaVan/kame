using backend.Models;
using backend.Common;
using backend.Shared.Enums;
namespace backend.Services.Interfaces
{
    public interface IBarService
    {
        Task<Bar?> GetDefaultBar();
        Task<Result<Bar?>> SetBarState(Guid BarId, BarState newState);
        Task<Result<BarUserEntry>> EnterBar(Guid bar, Guid user);
        Task<Result<BarUserEntry>> LeaveBar(Guid bar, Guid user);
        Task CheckSchedule(DateTime nowUtc);
        Task<List<Bar>> GetActiveBars();
    }
}