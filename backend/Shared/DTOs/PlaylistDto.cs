namespace backend.Shared.DTOs
{
    public class PlaylistDto
    {
        public Guid Id { get; set; }
        public Guid BarId { get; set; }
        public List<SongDto> Songs { get; set; } = new();
    }
}