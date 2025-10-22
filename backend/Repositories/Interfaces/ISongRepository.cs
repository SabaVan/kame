using backend.Models;

namespace backend.Repositories.Interfaces
{
    public interface ISongRepository
    {
        Task<IEnumerable<Song>> SearchAsync(string query, int limit = 20);
    }
}