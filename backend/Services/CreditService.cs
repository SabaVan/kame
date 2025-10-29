using backend.Models;
using backend.Repositories.Interfaces;
using backend.Utils.Errors;
using backend.Services.Interfaces;
using backend.Common;
using backend.Shared.Enums;
using System.Linq;
namespace backend.Services
{
    public class CreditService : ICreditService
    {
        private readonly IUserRepository _users;
        private readonly ITransactionRepository _transactions;
        private readonly IBarUserEntryRepository _barUserEntries;

        public CreditService(IUserRepository users, ITransactionRepository transactions, IBarUserEntryRepository barUserEntries)
        {
            ArgumentNullException.ThrowIfNull(users);
            ArgumentNullException.ThrowIfNull(transactions);
            ArgumentNullException.ThrowIfNull(barUserEntries);

            _users = users;
            _transactions = transactions;
            _barUserEntries = barUserEntries;
        }
        public async Task<Result<CreditTransaction>> AddCredits(Guid userId, int amount, string reason = "daily bonus", TransactionType type = TransactionType.Add)
        {
            var search_result = _users.GetUserById(userId);
            if (search_result.IsFailure)
                return Result<CreditTransaction>.Failure(StandardErrors.NotFound);

            User user = search_result.Value!;
            var bars = await _barUserEntries.GetBarsForUserAsync(userId);
            Bar? bar = bars?.FirstOrDefault();

            if (bar == null)
                return Result<CreditTransaction>.Failure(StandardErrors.NotFound);


            var updatedCredits = user.Credits;
            updatedCredits.Add(amount); // modifies copy
            user.Credits = updatedCredits; // overwrite property

            var update_result = _users.UpdateUser(user);

            if (update_result.IsFailure)
                return Result<CreditTransaction>.Failure(StandardErrors.TransactionErrorAdd);

            // if successful, log the transaction
            CreditTransaction transaction = new CreditTransaction
            {
                UserId = userId,
                Amount = amount,
                Reason = reason,
                Type = type
            };
            await _transactions.AddAsync(transaction);

            return Result<CreditTransaction>.Success(transaction);
        }

        public async Task<Result<CreditTransaction>> SpendCredits(Guid userId, int amount, string reason, TransactionType type = TransactionType.Spend)
        {
            var search_result = _users.GetUserById(userId);
            if (search_result.IsFailure)
                return Result<CreditTransaction>.Failure(StandardErrors.NotFound);

            User user = search_result.Value!;

            bool isSpendCreditsSuccessful = user.Credits.TrySpend(amount); // automatically spend credits when possible
            if (!isSpendCreditsSuccessful)
                return Result<CreditTransaction>.Failure(StandardErrors.InsufficientCredits);

            // Update user in database     
            var result = _users.UpdateUser(user);

            CreditTransaction transaction = new CreditTransaction
            {
                UserId = userId,
                Amount = amount,
                Reason = reason,
                Type = type
            };
            await _transactions.AddAsync(transaction);

            return Result<CreditTransaction>.Success(transaction);
        }

        public Result<int> GetBalance(Guid userId)
        {

            var search_result = _users.GetUserById(userId);
            if (search_result.IsFailure)
                return Result<int>.Failure(StandardErrors.NotFound);

            User user = search_result.Value!;

            return Result<int>.Success(user.Credits.Total);
        }

        public async Task<List<CreditTransaction>> GetLogsForUser(Guid userId)
        {
            var logs = await _transactions.GetByUserAsync(userId);
            return logs?.ToList() ?? new List<CreditTransaction>();
        }
    }
}