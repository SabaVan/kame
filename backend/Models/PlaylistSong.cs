namespace backend.Models
{
  public class PlaylistSong : IComparable<PlaylistSong>
  {
    public Guid Id { get; set; } = Guid.NewGuid();
    public required Guid PlaylistId { get; set; }
    public required Guid SongId { get; set; }
    public required Song Song { get; set; }
    public required Guid AddedByUserId { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.Now;
    public int CurrentBid { get; set; } // Highest bid
    public Guid? CurrentBidderId { get; set; }
    public int Position { get; set; }

    public void AddBid(int amount)
    {
      if (amount <= 0)
      {
        throw new ArgumentException("Bid amount must be positive", nameof(amount));
      }
      if (amount <= CurrentBid)
      {
        throw new ArgumentException("Bid amount must be higher than current bid", nameof(amount));
      }
      CurrentBid = amount;
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