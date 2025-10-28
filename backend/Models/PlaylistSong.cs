using backend.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    public class PlaylistSong : IComparable<PlaylistSong>
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public required Guid PlaylistId { get; set; }
        public required Guid SongId { get; set; }
        [NotMapped]
        public required Song Song { get; set; }
        public required Guid AddedByUserId { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.UtcNow; public int CurrentBid { get; set; } // Highest bid
        public Guid? CurrentBidderId { get; set; }
        public int Position { get; set; }

        public Result<int> AddBid(int amount)
        {
            if (amount <= 0)
                return Result<int>.Failure("INVALID_AMOUNT", "Bid amount must be positive.");

            if (amount <= CurrentBid)
                return Result<int>.Failure("LOWER_THAN_CURRENT", "Bid must be higher than current bid.");

            CurrentBid = amount;
            return Result<int>.Success(CurrentBid);
        }

        public int CompareTo(PlaylistSong? other)
        {
            if (other == null) return 1;

            int bidComparison = other.CurrentBid.CompareTo(CurrentBid);

            if (bidComparison != 0)
            {
                return bidComparison;
            }
            else
            {
                int addedAtComparison = other.AddedAt.CompareTo(AddedAt);
                if (addedAtComparison != 0)
                {
                    return addedAtComparison;
                }
                return 0;
            }
        }
    }
}