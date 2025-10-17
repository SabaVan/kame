using backend.Common;
using backend.Models;
namespace backend.Repositories.Interfaces
{
    public interface IBarUserEntryRepository
    {
        Task<Result<BarUserEntry>> AddEntryAsync(BarUserEntry entry);
        Task<Result<BarUserEntry>> RemoveEntryAsync(Guid barId, Guid userId);
        Task<Result<BarUserEntry>> RemoveEntryAsync(BarUserEntry entry);
        Task<List<User>> GetUsersInBarAsync(Guid barId);
        Task<List<Bar>> GetBarsForUserAsync(Guid userId);
        Task<Result<BarUserEntry>> FindEntryAsync(Guid barId, Guid userId);
        Task SaveChangesAsync();
    }
}