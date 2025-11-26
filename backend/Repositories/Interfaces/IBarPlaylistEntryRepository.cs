using backend.Common;
using backend.Models;
namespace backend.Repositories.Interfaces
{
    public interface IBarPlaylistEntryRepository
    {
        Task<Result<BarPlaylistEntry>> AddEntryAsync(Guid barId, Guid playlistId);
        Task<Result<BarPlaylistEntry>> AddEntryAsync(Bar bar, Playlist playlist);
        Task<Result<BarPlaylistEntry>> AddEntryAsync(BarPlaylistEntry entry);
        Task<Result<BarPlaylistEntry>> RemoveEntryAsync(Guid barId, Guid playlistId);
        Task<Result<BarPlaylistEntry>> RemoveEntryAsync(Bar bar, Playlist playlist);
        Task<Result<BarPlaylistEntry>> RemoveEntryAsync(BarPlaylistEntry entry);
        Task<List<Playlist>> GetPlaylistsForBarAsync(Guid barId);
        Task<List<Bar>> GetBarsForPlaylistAsync(Guid playlistId);
        Task<Result<BarPlaylistEntry>> FindEntryAsync(Guid barId, Guid playlistId);
        Task<Result<BarPlaylistEntry>> FindEntryAsync(Bar bar, Playlist playlist);
        Task<Result<BarPlaylistEntry>> FindEntryAsync(BarPlaylistEntry entry);
    }
}