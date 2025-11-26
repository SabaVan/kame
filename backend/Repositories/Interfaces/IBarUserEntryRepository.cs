using backend.Common;
using backend.Models;
namespace backend.Repositories.Interfaces
{
    public interface IBarUserEntryRepository
    {
        Task<Result<BarUserEntry>> AddEntryAsync(Guid barId, Guid userId);
        Task<Result<BarUserEntry>> AddEntryAsync(Bar bar, User user);
        Task<Result<BarUserEntry>> AddEntryAsync(BarUserEntry entry);
        Task<Result<BarUserEntry>> RemoveEntryAsync(Guid barId, Guid userId);
        Task<Result<BarUserEntry>> RemoveEntryAsync(Bar bar, User user);
        Task<Result<BarUserEntry>> RemoveEntryAsync(BarUserEntry entry);
        Task<List<User>> GetUsersInBarAsync(Guid barId);
        Task<List<Bar>> GetBarsForUserAsync(Guid userId);
        Task<Result<BarUserEntry>> FindEntryAsync(Guid barId, Guid userId);
        Task<Result<BarUserEntry>> FindEntryAsync(Bar bar, User user);
        Task<Result<BarUserEntry>> FindEntryAsync(BarUserEntry entry);
        Task SaveChangesAsync();
        Task<List<Guid>> GetAllUniqueBarIdsAsync();
        Task TouchEntryAsync(Guid barId, Guid userId);
        Task<List<BarUserEntry>> GetEntriesOlderThanAsync(DateTime cutoffUtc);
    }
}