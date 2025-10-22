using backend.Data;
using backend.Models;
using backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore; // ToListAsync()
using backend.Common;
using backend.Utils.Errors;
namespace backend.Repositories
{
    public class BarPlaylistEntryRepository : IBarPlaylistEntryRepository
    {
        private readonly AppDbContext _context;
        public BarPlaylistEntryRepository(AppDbContext context)
        {
            _context = context;
        }
        /// <summary>
        /// Returns Result<BarPlaylistEntry>.Success(entry) if added new entry.
        /// Returns Result<BarPlaylistEntry>.Failure(StandartErrors.AlreadyExists) if entry already exists.
        /// </summary>
        public async Task<Result<BarPlaylistEntry>> AddEntryAsync(Guid barId, Guid PlaylistId)
        {
            bool exists = await _context.BarPlaylistEntries
                .AnyAsync(e => e.BarId == barId && e.PlaylistId == PlaylistId);

            if (exists)
                return Result<BarPlaylistEntry>.Failure(StandardErrors.EntryAlreadyExists);

            var entry = new BarPlaylistEntry(barId, PlaylistId);

            await _context.BarPlaylistEntries.AddAsync(entry);
            return Result<BarPlaylistEntry>.Success(entry);
        }
        public async Task<Result<BarPlaylistEntry>> AddEntryAsync(Bar bar, Playlist playlist)
        {
            return await AddEntryAsync(bar.Id, playlist.Id);
        }
        public async Task<Result<BarPlaylistEntry>> AddEntryAsync(BarPlaylistEntry entry)
        {
            return await AddEntryAsync(entry.BarId, entry.PlaylistId);
        }

        public async Task<Result<BarPlaylistEntry>> RemoveEntryAsync(Guid barId, Guid PlaylistId)
        {
            var entry = await _context.BarPlaylistEntries
                .FirstOrDefaultAsync(e => e.BarId == barId && e.PlaylistId == PlaylistId);

            if (entry == null)
                return Result<BarPlaylistEntry>.Failure(StandardErrors.NonexistentEntry);

            _context.BarPlaylistEntries.Remove(entry);
            return Result<BarPlaylistEntry>.Success(entry);
        }
        public async Task<Result<BarPlaylistEntry>> RemoveEntryAsync(Bar bar, Playlist playlist)
        {
            return await RemoveEntryAsync(bar.Id, playlist.Id);
        }
        public async Task<Result<BarPlaylistEntry>> RemoveEntryAsync(BarPlaylistEntry entry)
        {
            return await RemoveEntryAsync(entry.BarId, entry.PlaylistId);
        }
                public async Task<List<Playlist>> GetPlaylistsForBarAsync(Guid barId)
        {
            return await _context.Playlists
                .Where(u => _context.BarPlaylistEntries
                    .Any(e => e.BarId == barId && e.PlaylistId == u.Id))
                .ToListAsync();
        }

        public async Task<List<Bar>> GetBarsForPlaylistAsync(Guid PlaylistId)
        {
            return await _context.Bars
                .Where(b => _context.BarPlaylistEntries
                    .Any(e => e.PlaylistId == PlaylistId && e.BarId == b.Id))
                .ToListAsync();
        }

        public async Task<Result<BarPlaylistEntry>> FindEntryAsync(Guid barId, Guid PlaylistId)
        {
            var entry = await _context.BarPlaylistEntries
                .FindAsync(barId, PlaylistId);

            if (entry == null)
                return Result<BarPlaylistEntry>.Failure(StandardErrors.NonexistentEntry);

            return Result<BarPlaylistEntry>.Success(entry);
        }
        public async Task<Result<BarPlaylistEntry>> FindEntryAsync(Bar bar, Playlist playlist)
        {
            var entry = await _context.BarPlaylistEntries
                .FindAsync(bar.Id, playlist.Id);

            if (entry == null)
                return Result<BarPlaylistEntry>.Failure(StandardErrors.NonexistentEntry);

            return Result<BarPlaylistEntry>.Success(entry);
        }
        public async Task<Result<BarPlaylistEntry>> FindEntryAsync(BarPlaylistEntry entry)
        {
            return await FindEntryAsync(entry.BarId, entry.PlaylistId);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}