
using backend.Models;
using backend.Common;

namespace backend.Services.Interfaces
{
    public interface IPlaylistService
    {
        Result<PlaylistSong> AddSong(Guid userId, Song song);
        Result<Bid> BidOnSong(Guid userId, Guid songId, int amount);
        Result<Song> GetNextSong(Guid playlistId);
    }
}