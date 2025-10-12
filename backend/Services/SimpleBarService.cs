using System;
using System.Collections.Generic;
using System.Threading.Tasks;
// using Microsoft.EntityFrameworkCore;   // Required for EF Core async methods (future DB)
using backend.Models;                  // Your Bar model
// using backend.Data;                    // Your AppDbContext (future DB)

namespace backend.Services
{
    public class SimpleBarService
    {
        // private readonly AppDbContext _context; // future DB

        // For now, you can use an in-memory list
        private readonly List<Bar> _bars = new();

        // FUTURE DB constructor
        // public SimpleBarService(AppDbContext context)
        // {
        //     _context = context;
        // }

        // In-memory constructor
        public SimpleBarService()
        {
        }

        // Get all Bars
        public Task<List<Bar>> GetAllBarsAsync()
        {
            // FUTURE DB: return await _context.Bars.ToListAsync();
            return Task.FromResult(_bars.ToList());
        }

        // Get a Bar by its GUID
        public Task<Bar?> GetBarByIdAsync(Guid id)
        {
            // FUTURE DB: return await _context.Bars.FindAsync(id);
            var bar = _bars.FirstOrDefault(b => b.Id == id);
            return Task.FromResult(bar);
        }

        // Add a new Bar
        public Task<Bar> AddBarAsync(Bar bar)
        {
            bar.Id = Guid.NewGuid();
            _bars.Add(bar);

            // FUTURE DB:
            // _context.Bars.Add(bar);
            // await _context.SaveChangesAsync();

            return Task.FromResult(bar);
        }
        // Optional: Delete a Bar
        public Task<bool> DeleteBarAsync(Guid id)
        {
            var bar = _bars.FirstOrDefault(b => b.Id == id);
            if (bar == null) return Task.FromResult(false);

            _bars.Remove(bar);

            // FUTURE DB:
            // _context.Bars.Remove(bar);
            // await _context.SaveChangesAsync();

            return Task.FromResult(true);
        }
    }
}
