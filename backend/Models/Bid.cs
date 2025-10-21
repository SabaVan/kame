namespace backend.Models
{
    public class Bid
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public required Guid UserId { get; set; }
        public required Guid PlaylistSongId { get; set; }
        public required int Amount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsRefunded { get; set; } = false;
    }
}