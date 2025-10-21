using backend.Shared.Enums;

namespace backend.Models
{
    public record CreditTransaction
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public Guid UserId { get; init; }
        public int Amount { get; init; }
        public required string Reason { get; init; }
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
        public TransactionType Type { get; init; }
    }

}