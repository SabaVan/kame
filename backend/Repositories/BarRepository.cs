using backend.Repositories.Interfaces;
using backend.Models;
using backend.Data;
using Microsoft.EntityFrameworkCore; // ToListAsync()
namespace backend.Repositories
{
    public class BarRepository : IBarRepository
    {
        private readonly AppDbContext _context;
        public BarRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<List<Bar>> GetAllAsync()
        {
            return await _context.Bars.ToListAsync();
        }
        public async Task<Bar?> GetByIdAsync(Guid id)
        {
            return await _context.Bars.FindAsync(id);
        }
        public async Task AddAsync(Bar bar)
        {
            // Note: Do NOT save here; let caller call SaveChangesAsync()
            await _context.Bars.AddAsync(bar);
        }
        public async Task<Bar?> UpdateAsync(Bar bar)
        {
            var existing = await _context.Bars.FindAsync(bar.Id);
            if (existing == null)
                return null; // not found

            existing.SetState(bar.State);
            existing.SetSchedule(bar._openAtUtc, bar._closeAtUtc);
            existing.Name = bar.Name;

            _context.Bars.Update(existing); // mark update
            // Note: Do NOT save here; let caller call SaveChangesAsync()
            return existing;
        }
        public async Task<bool> DeleteAsync(Guid id)
        {
            var bar = await _context.Bars.FindAsync(id);
            if (bar == null) return false;  // entity does not exist
            _context.Bars.Remove(bar);
            // Note: Do NOT save here; let caller call SaveChangesAsync()
            return true; // deletion succeeded 
        }
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}