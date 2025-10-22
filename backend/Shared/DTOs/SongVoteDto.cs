namespace backend.Shared.DTOs
{
    public class SongVoteDto
    {
        public Guid SongId { get; set; }
        public bool Upvote { get; set; }
    }
}