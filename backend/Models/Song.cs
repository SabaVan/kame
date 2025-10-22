namespace backend.Models
{
    public class Song
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public required string Title { get; set; }
        public required string Artist { get; set; }
        public TimeSpan? Duration { get; set; }
        public string? Album { get; set; }
        public string? StreamUrl { get; set; }
    }
}