using backend.Models;
namespace backend.Repositories.Interfaces
{
    public interface IBarRepository
    {
        Task<List<Bar>> GetAllAsync();
        Task<Bar?> GetByIdAsync(Guid id);
        Task AddAsync(Bar bar);
        Task<Bar?> UpdateAsync(Bar bar);
        Task<bool> DeleteAsync(Guid id);
        Task SaveChangesAsync();
    }
}