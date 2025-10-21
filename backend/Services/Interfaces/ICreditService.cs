using backend.Common;
using backend.Models;
using backend.Shared.Enums;
namespace backend.Repositories.Interfaces
{
    public interface ICreditService
    {
        Task<Result<CreditTransaction>> AddCredits(Guid userId, int amount, string reason, TransactionType type);
        Task<Result<CreditTransaction>> SpendCredits(Guid userId, int amount, string reason, TransactionType type);
        Task<Result<int>> GetBalance(Guid userId);
        Task<List<CreditTransaction>> GetLogsForUser(Guid userId);
    }
}
