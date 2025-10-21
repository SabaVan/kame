using backend.Shared.Enums;

namespace backend.Models
{
    public record CreditTransaction(
        Guid Id,
        Guid UserId,
        int Amount,
        string Reason,
        DateTime CreatedAt,
        TransactionType Type
    );
}