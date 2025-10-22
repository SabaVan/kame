using backend.Data;
using backend.Models;
using backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore; // ToListAsync()
using backend.Common;
using backend.Utils.Errors;
namespace backend.Repositories
{
    public class BarUserEntryRepository : IBarUserEntryRepository
    {
        private readonly AppDbContext _context;
        public BarUserEntryRepository(AppDbContext context)
        {
            _context = context;
        }
        /// <summary>
        /// Returns Result<BarUserEntry>.Success(entry) if added new entry.
        /// Returns Result<BarUserEntry>.Failure(StandartErrors.AlreadyExists) if entry already exists.
        /// </summary>
        public async Task<Result<BarUserEntry>> AddEntryAsync(Guid barId, Guid userId)
        {
            bool exists = await _context.BarUserEntries
                .AnyAsync(e => e.BarId == barId && e.UserId == userId);

            if (exists)
                return Result<BarUserEntry>.Failure(StandardErrors.EntryAlreadyExists);

            var entry = new BarUserEntry(barId, userId);

            await _context.BarUserEntries.AddAsync(entry);
            return Result<BarUserEntry>.Success(entry);
        }
        public async Task<Result<BarUserEntry>> AddEntryAsync(Bar bar, User user)
        {
            return await AddEntryAsync(bar.Id, user.Id);
        }
        public async Task<Result<BarUserEntry>> AddEntryAsync(BarUserEntry entry)
        {
            return await AddEntryAsync(entry.BarId, entry.UserId);
        }

        public async Task<Result<BarUserEntry>> RemoveEntryAsync(Guid barId, Guid userId)
        {
            var entry = await _context.BarUserEntries
                .FirstOrDefaultAsync(e => e.BarId == barId && e.UserId == userId);

            if (entry == null)
                return Result<BarUserEntry>.Failure(StandardErrors.NonexistentEntry);

            _context.BarUserEntries.Remove(entry);
            return Result<BarUserEntry>.Success(entry);
        }
        public async Task<Result<BarUserEntry>> RemoveEntryAsync(Bar bar, User user)
        {
            return await RemoveEntryAsync(bar.Id, user.Id);
        }
        public async Task<Result<BarUserEntry>> RemoveEntryAsync(BarUserEntry entry)
        {
            return await RemoveEntryAsync(entry.BarId, entry.UserId);
        }
        // change to List<User>
        public async Task<List<User>> GetUsersInBarAsync(Guid barId)
        {
            return await _context.Users
                .Where(u => _context.BarUserEntries
                    .Any(e => e.BarId == barId && e.UserId == u.Id))
                .ToListAsync();
        }

        public async Task<List<Bar>> GetBarsForUserAsync(Guid userId)
        {
            return await _context.Bars
                .Where(b => _context.BarUserEntries
                    .Any(e => e.UserId == userId && e.BarId == b.Id))
                .ToListAsync();
        }

        public async Task<Result<BarUserEntry>> FindEntryAsync(Guid barId, Guid userId)
        {
            var entry = await _context.BarUserEntries
                .FindAsync(barId, userId);

            if (entry == null)
                return Result<BarUserEntry>.Failure(StandardErrors.NonexistentEntry);

            return Result<BarUserEntry>.Success(entry);
        }
        public async Task<Result<BarUserEntry>> FindEntryAsync(Bar bar, User user)
        {
            var entry = await _context.BarUserEntries
                .FindAsync(bar.Id, user.Id);

            if (entry == null)
                return Result<BarUserEntry>.Failure(StandardErrors.NonexistentEntry);

            return Result<BarUserEntry>.Success(entry);
        }
        public async Task<Result<BarUserEntry>> FindEntryAsync(BarUserEntry entry)
        {
            return await FindEntryAsync(entry.BarId, entry.UserId);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}