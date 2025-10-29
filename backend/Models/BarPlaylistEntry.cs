using System.ComponentModel.DataAnnotations.Schema;
using backend.Models;
namespace backend.Models
{
    public class BarPlaylistEntry
    {
        public Guid BarId { get; set; }
        public Guid PlaylistId { get; set; }
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public BarPlaylistEntry() { }
        public BarPlaylistEntry(Bar Bar, Playlist Playlist)
        {
            BarId = Bar.Id;
            PlaylistId = Playlist.Id;
        }
        public BarPlaylistEntry(Guid BarId, Guid PlaylistId)
        {
            this.BarId = BarId;
            this.PlaylistId = PlaylistId;
        }
    }
}