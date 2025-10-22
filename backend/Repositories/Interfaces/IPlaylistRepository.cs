using backend.Models;
using backend.Common;

namespace backend.Repositories.Interfaces
{
    public interface IPlaylistRepository
    {
        Task AddAsync(Playlist playlist);
        Task<Playlist?> GetActivePlaylistAsync();
        Task<PlaylistSong?> GetPlaylistSongBySongIdAsync(Guid songId);
        Task<Playlist?> GetByIdAsync(Guid id);
        Task UpdatePlaylistSongAsync(PlaylistSong song);
        Task UpdateAsync(Playlist playlist);
        Task RemoveAsync(Playlist playlist);
    }
}
