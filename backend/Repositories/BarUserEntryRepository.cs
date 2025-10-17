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
        // Success - entity was removed
        // Failure - entity does not exist
        public async Task<Result<BarUserEntry>> RemoveEntryAsync(BarUserEntry entry)
        {
            if (entry is null)
                return Result<BarUserEntry>.Failure(StandardErrors.NonexistentEntity);

            // Try to find the entity in the DB
            var existing = await _context.BarUserEntries
                .FindAsync(entry.BarId, entry.UserId);

            if (existing is null)
                return Result<BarUserEntry>.Failure(StandardErrors.NonexistentEntity);

            _context.BarUserEntries.Remove(existing);

            // Return success with the removed entity
            return Result<BarUserEntry>.Success(existing);
        }

        public async Task<Result<BarUserEntry>> AddEntryAsync(BarUserEntry entry)
        {
            bool exists = await _context.BarUserEntries
                .AnyAsync(e => e.BarId == entry.BarId && e.UserId == entry.UserId);

            if (exists)
                return Result<BarUserEntry>.Failure(StandardErrors.AlreadyExists);

            await _context.BarUserEntries.AddAsync(entry);
            return Result<BarUserEntry>.Success(entry);
        }

        public async Task<Result<BarUserEntry>> RemoveEntryAsync(Guid barId, Guid userId)
        {
            var entry = await _context.BarUserEntries
                .FirstOrDefaultAsync(e => e.BarId == barId && e.UserId == userId);

            if (entry is null)
                return Result<BarUserEntry>.Failure(StandardErrors.NonexistentEntity);

            _context.BarUserEntries.Remove(entry);
            // Note: Do NOT save here; let caller call SaveChangesAsync()
            return Result<BarUserEntry>.Success(entry);
        }


        // change to List<User>
        public async Task<List<User>> GetUsersInBarAsync(Guid barId)
        {
            return await _context.BarUserEntries.Where(e => (e.BarId == barId)).Select(e => e.User).ToListAsync();
        }
        public async Task<List<Bar>> GetBarsForUserAsync(Guid userId)
        {
            return await _context.BarUserEntries.Where(e => e.UserId == userId).Select(e => e.Bar).ToListAsync();
        }
        public async Task<Result<BarUserEntry>> FindEntryAsync(Guid barId, Guid userId)
        {
            var entry = await _context.BarUserEntries.FindAsync(barId, userId);
            if (entry == null)
                return Result<BarUserEntry>.Failure(StandardErrors.NonexistentEntity);

            return Result<BarUserEntry>.Success(entry);
        }
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}