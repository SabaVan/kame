using backend.Common;
using backend.Models;
namespace backend.Repositories.Interfaces
{
    public interface ITransactionRepository
    {
        Task<Result<CreditTransaction>> AddAsync(CreditTransaction creditTransaction);
        Task<IEnumerable<CreditTransaction>> GetByUserAsync(Guid userId);
        Task<IEnumerable<CreditTransaction>> GetByBarAsync(Guid barId);
        Task<IEnumerable<CreditTransaction>> GetAllAsync();
    }
}