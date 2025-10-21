using backend.Data;
using backend.Models;
using backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories
{
    public class PlaylistRepository : IPlaylistRepository
    {
        private readonly AppDbContext _context;

        public PlaylistRepository(AppDbContext context)
        {
            _context = context;
        }

        public void Add(Playlist playlist)
        {
            _context.Playlists.Add(playlist);
            _context.SaveChanges();
        }

        public Playlist? GetActivePlaylist()
        {
            return _context.Playlists
                .Include(p => p.Songs)
                .ThenInclude(ps => ps.Song)
                .OrderByDescending(p => p.Id)
                .FirstOrDefault();
        }

        public PlaylistSong? GetPlaylistSongBySongId(Guid songId)
        {
            return _context.PlaylistSongs
                .Include(ps => ps.Song)
                .FirstOrDefault(ps => ps.SongId == songId);
        }

        public Playlist? GetById(Guid id)
        {
            return _context.Playlists
                .Include(p => p.Songs)
                .ThenInclude(ps => ps.Song)
                .FirstOrDefault(p => p.Id == id);
        }

        public void UpdatePlaylistSong(PlaylistSong song)
        {
            _context.PlaylistSongs.Update(song);
            _context.SaveChanges();
        }

        public void Update(Playlist playlist)
        {
            _context.Playlists.Update(playlist);
            _context.SaveChanges();
        }

        public void Remove(Playlist playlist)
        {
            _context.Playlists.Remove(playlist);
            _context.SaveChanges();
        }
    }
}