namespace backend.Shared.DTOs
{

    public struct SongDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public int Votes { get; set; }
        public int CurrentBid { get; set; }
        public int Position { get; set; }

    }
}