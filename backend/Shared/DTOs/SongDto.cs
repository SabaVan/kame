namespace backend.Shared.DTOs
{
    public class SongDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public int Votes { get; set; }
        public int CurrentBid { get; set; } = 0;

    }
}