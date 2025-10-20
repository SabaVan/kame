using backend.Models;
using backend.Common;
using backend.Shared.Enums;
namespace backend.Services.Interfaces
{
    public interface IBarService
    {
        Task<Bar?> GetDefaultBar();
        Task<Result<Bar?>> SetBarState(Guid BarId, BarState newState);
        Task<Result<BarUserEntry>> EnterBar(Bar bar, User user);
        Task<Result<BarUserEntry>> LeaveBar(Bar bar, User user);
        Task CheckSchedule(DateTime nowUtc);
    }
}