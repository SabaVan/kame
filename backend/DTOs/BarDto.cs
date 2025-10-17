namespace backend.DTOs
{
    public class BarDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;  // "Open", "Closed", etc.
        public string OpenAt { get; set; } = string.Empty; // formatted string
        public string CloseAt { get; set; } = string.Empty; // formatted string
        public string CurrentPlaylist { get; set; } = string.Empty;
    }
}
