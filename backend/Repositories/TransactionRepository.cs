using backend.Data;
using backend.Models;
using backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using backend.Common;
using backend.Utils.Errors;

namespace backend.Repositories
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly AppDbContext _context;
        public TransactionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Result<CreditTransaction>> AddAsync(CreditTransaction creditTransaction)
        {
            bool exists = await _context.CreditTransactions.AnyAsync(t =>
              t.UserId == creditTransaction.UserId &&
              t.Amount == creditTransaction.Amount &&
              t.Reason == creditTransaction.Reason &&
              t.Type == creditTransaction.Type &&
              t.CreatedAt == creditTransaction.CreatedAt
            );

            if (exists)
                return Result<CreditTransaction>.Failure(StandardErrors.AlreadyExists);

            await _context.CreditTransactions.AddAsync(creditTransaction);
            return Result<CreditTransaction>.Success(creditTransaction);
        }

        public async Task<IEnumerable<CreditTransaction>> GetByUserAsync(Guid userId)
        {
            return await _context.CreditTransactions
              .Where(t => t.UserId == userId)
              .ToListAsync();
        }

        public async Task<IEnumerable<CreditTransaction>> GetByBarAsync(Guid barId)
        {
            return await _context.CreditTransactions
              .Where(t => t.BarId == barId)
              .ToListAsync();
        }

        public async Task<IEnumerable<CreditTransaction>> GetAllAsync()
        {
            return await _context.CreditTransactions.ToListAsync();
        }
    }
}