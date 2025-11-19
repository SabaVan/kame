using backend.Models;
using backend.Common;

namespace backend.Repositories.Interfaces
{
    public interface IPlaylistRepository
    {
        Task AddAsync(Playlist playlist);
        Task<PlaylistSong?> GetPlaylistSongBySongIdAsync(Guid songId);
        Task<Playlist?> GetByIdAsync(Guid id);
        Task UpdatePlaylistSongAsync(PlaylistSong song);
        Task UpdateAsync(Playlist playlist);
        Task RemoveAsync(Playlist playlist);
        Task<Song?> GetSongByIdAsync(Guid songId);
        Task AddSongAsync(Song song);
        Task AddPlaylistSongAsync(PlaylistSong playlistSong);

    }
}
