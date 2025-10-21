using backend.Models;

namespace backend.Repositories.Interfaces
{
    public interface IPlaylistRepository
    {
        void Add(Playlist playlist);
        Playlist? GetActivePlaylist();
        PlaylistSong? GetPlaylistSongBySongId(Guid songId);
        Playlist? GetById(Guid id);
        void UpdatePlaylistSong(PlaylistSong song);
        void Update(Playlist playlist);
        void Remove(Playlist playlist);
    }
}