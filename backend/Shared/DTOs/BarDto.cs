namespace backend.Shared.DTOs
{
public class BarDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public DateTime OpenAtUtc { get; set; }  // actual UTC value 
    public DateTime CloseAtUtc { get; set; } // actual UTC value
    public string CurrentPlaylist { get; set; } = string.Empty;
}

}
