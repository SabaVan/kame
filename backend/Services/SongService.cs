using backend.Models;
using backend.Repositories.Interfaces;
using backend.Services.Interfaces;

namespace backend.Services
{
    public class SongService : ISongService
    {
        private readonly ISongRepository _externalRepo;

        public SongService(ISongRepository externalRepo)
        {
            _externalRepo = externalRepo;
        }

        public async Task<IEnumerable<Song>> SearchSongsAsync(string query, int limit = 20)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Enumerable.Empty<Song>();

            return await _externalRepo.SearchAsync(query, limit);
        }
    }
}