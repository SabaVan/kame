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
    }
}
