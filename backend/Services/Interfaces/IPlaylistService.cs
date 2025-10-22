using backend.Models;
using backend.Common;

namespace backend.Services.Interfaces
{
    public interface IPlaylistService
    {
        Task<Result<PlaylistSong>> AddSongAsync(Guid userId, Song song);
        Task<Result<Bid>> BidOnSongAsync(Guid userId, Guid songId, int amount);
        Task<Result<Song>> GetNextSongAsync(Guid playlistId);
        Task<Result<Playlist>> ReorderAndSavePlaylistAsync(Guid playlistId);
        Task<Result<Playlist>> GetByIdAsync(Guid playlistId);
    }
}