using backend.Models;

namespace backend.Services.Interfaces
{
    public interface ISongService
    {
        Task<IEnumerable<Song>> SearchSongsAsync(string query, int limit = 20);
    }
}