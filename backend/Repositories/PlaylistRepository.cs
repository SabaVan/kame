using backend.Data;
using backend.Models;
using backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using backend.Common;
using backend.Utils.Errors;

namespace backend.Repositories
{
    public class PlaylistRepository : IPlaylistRepository
    {
        private readonly AppDbContext _context;

        public PlaylistRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Playlist playlist)
        {
            _context.Playlists.Add(playlist);
            await _context.SaveChangesAsync();
        }

        public async Task<Playlist?> GetActivePlaylistAsync()
        {
            return await _context.Playlists
                .Include(p => p.Songs)
                .ThenInclude(ps => ps.Song)
                .OrderByDescending(p => p.Id)
                .FirstOrDefaultAsync();
        }

        public async Task<PlaylistSong?> GetPlaylistSongBySongIdAsync(Guid songId)
        {
            return await _context.PlaylistSongs
                .Include(ps => ps.Song)
                .FirstOrDefaultAsync(ps => ps.SongId == songId);
        }

        public async Task<Playlist?> GetByIdAsync(Guid id)
        {
            return await _context.Playlists
                .Include(p => p.Songs)
                .ThenInclude(ps => ps.Song)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task AddSongAsync(Song song)
        {
            _context.Songs.Add(song);
            await _context.SaveChangesAsync();
        }

        public async Task AddPlaylistSongAsync(PlaylistSong playlistSong)
        {
            _context.PlaylistSongs.Add(playlistSong);
            await _context.SaveChangesAsync();
        }

        public async Task<Song?> GetSongByIdAsync(Guid songId)
        {
            return await _context.Songs.FirstOrDefaultAsync(s => s.Id == songId);
        }

        public async Task UpdatePlaylistSongAsync(PlaylistSong song)
        {
            _context.PlaylistSongs.Update(song);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Playlist playlist)
        {
            _context.Playlists.Update(playlist);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveAsync(Playlist playlist)
        {
            _context.Playlists.Remove(playlist);
            await _context.SaveChangesAsync();
        }

        public async Task<Result<PlaylistSong>> SearchSongAsync(Guid playlistId, string? artist = null, string? title = null)
        {
            // Fetch the playlist with its songs
            var playlist = await _context.Playlists
                .Include(p => p.Songs)
                .ThenInclude(ps => ps.Song)
                .FirstOrDefaultAsync(p => p.Id == playlistId);

            if (playlist == null)
                return Result<PlaylistSong>.Failure(StandardErrors.NotFound);

            // Filter songs by title and/or artist
            var query = playlist.Songs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(artist))
                query = query.Where(ps => ps.Song.Artist.Contains(artist, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(title))
                query = query.Where(ps => ps.Song.Title.Contains(title, StringComparison.OrdinalIgnoreCase));

            // Return the first matching song or a not found result
            var matchedSong = query.FirstOrDefault();
            return matchedSong != null
                ? Result<PlaylistSong>.Success(matchedSong)
                : Result<PlaylistSong>.Failure(StandardErrors.NotFound);
        }
    }
}
