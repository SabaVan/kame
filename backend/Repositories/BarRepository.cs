using backend.Repositories.Interfaces;
using backend.Models;
using backend.Data;
namespace backend.Repositories
{
    public class BarRepository : IBarRepository
    {
        private readonly List<Bar> _bars = new();
        
/*         // private readonly AppDbContext _context;
        public BarRepository(AppDbContext context)
        {
            _context = context;
        } */
        public Task<List<Bar>> GetAllAsync()
        {
            // return await _Bars.ToListAsync() 
            return Task.FromResult(_bars.ToList());
        }
        public Task<Bar?> GetByIdAsync(Guid id)
        {
            // return await _context.Bars.FindAsync(id); 
            Bar? bar = _bars.Find(b => b.Id == id);
            return Task.FromResult(bar);
        }
        public Task AddAsync(Bar bar)
        {
            // await _context.Bars.AddAsync(bar);
            // await _context.SaveChangesAsync();
            _bars.Add(bar);
            return Task.CompletedTask;
        }
        public Task<Bar?> UpdateAsync(Bar bar)
        {    
            /* var existing = await _context.Bars.FindAsync(bar.Id);
            if (existing == null) 
                return null; // not found
            existing.SetState(bar.State);
            existing.OpenAt = bar.OpenAt;
            existing.CloseAt = bar.CloseAt;

             _context.Bars.Update(existing); // mark update
            await _context.SaveChangesAsync();
            return existing; */
            
            var existing = _bars.Find(b => b.Id == bar.Id);
            if (existing == null) 
                return Task.FromResult<Bar?>(null);

            existing.SetState(bar.State);
            existing.OpenAt = bar.OpenAt;
            existing.CloseAt = bar.CloseAt;
            
            return Task.FromResult<Bar?>(existing);
        }
        public Task<bool> DeleteAsync(Guid id)
        {
            /* var bar = await _context.Bars.FindAsync(id);
            if (bar == null) return false;  // entity does not exist
            _context.Bars.Remove(bar);
            await _context.SaveChangesAsync();
            return true; // deletion succeeded */
            int removed = _bars.RemoveAll(b => b.Id == id);
            return Task.FromResult(removed > 0);
        }
    }
}