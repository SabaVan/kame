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
        public async Task<Result<BarUserEntry>> AddEntryAsync(Guid barId, Guid userId)
        {
            if (!await _context.Bars.AnyAsync(b => b.Id == barId))
                return Result<BarUserEntry>.Failure(StandardErrors.NonexistentBar);

            if (!await _context.Users.AnyAsync(u => u.Id == userId))
                return Result<BarUserEntry>.Failure(StandardErrors.NonexistentUser);

            bool exists = await _context.BarUserEntries
                .AnyAsync(e => e.BarId == barId && e.UserId == userId);

            if (exists)
                return Result<BarUserEntry>.Failure(StandardErrors.EntryAlreadyExists);

            var entry = new BarUserEntry(barId, userId);
            _context.BarUserEntries.Add(entry);
            await _context.SaveChangesAsync();

            return Result<BarUserEntry>.Success(entry);
        }
        public async Task<Result<BarUserEntry>> AddEntryAsync(Bar bar, User user)
        {
            return await AddEntryAsync(bar.Id, user.Id); // Uses the overload: AddEntryAsync(Guid barId, Guid userId)
        }
        public async Task<Result<BarUserEntry>> AddEntryAsync(BarUserEntry entry)
        {
            bool exists = await _context.BarUserEntries
                .AnyAsync(e => e.BarId == entry.BarId && e.UserId == entry.UserId);

            if (exists)
                return Result<BarUserEntry>.Failure(StandardErrors.EntryAlreadyExists);

            // Preserve the provided EnteredAt if set
            await _context.BarUserEntries.AddAsync(entry);
            await _context.SaveChangesAsync();
            return Result<BarUserEntry>.Success(entry);
        }

        public async Task<Result<BarUserEntry>> RemoveEntryAsync(Guid barId, Guid userId)
        {
            var entry = await _context.BarUserEntries
                .FirstOrDefaultAsync(e => e.BarId == barId && e.UserId == userId);

            if (entry == null)
                return Result<BarUserEntry>.Failure(StandardErrors.NonexistentEntry);

            _context.BarUserEntries.Remove(entry);
            await _context.SaveChangesAsync();
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
        public async Task<List<Guid>> GetAllUniqueBarIdsAsync()
        {
            return await _context.BarUserEntries
                .Select(e => e.BarId)
                .Distinct()
                .ToListAsync();
        }

        public async Task TouchEntryAsync(Guid barId, Guid userId)
        {
            var entry = await _context.BarUserEntries
                .FirstOrDefaultAsync(e => e.BarId == barId && e.UserId == userId);

            if (entry == null)
            {
                // If no entry exists, create one (user is effectively entering)
                var newEntry = new BarUserEntry(barId, userId) { EnteredAt = DateTime.UtcNow };
                await _context.BarUserEntries.AddAsync(newEntry);
                await _context.SaveChangesAsync();
                return;
            }

            entry.EnteredAt = DateTime.UtcNow;
            // Rely on EF change tracking to detect the modified property and persist it.
            await _context.SaveChangesAsync();
        }

        public async Task<List<BarUserEntry>> GetEntriesOlderThanAsync(DateTime cutoffUtc)
        {
            return await _context.BarUserEntries
                .Where(e => e.EnteredAt < cutoffUtc)
                .ToListAsync();
        }
    }
}