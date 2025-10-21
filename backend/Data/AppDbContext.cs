using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<Bar> Bars { get; set; }
        public DbSet<BarUserEntry> BarUserEntries { get; set; }
        public DbSet<User> Users { get; set; }
        //public DbSet<Song> Songs { get; set; }
        public DbSet<Playlist> Playlists { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<BarUserEntry>().HasKey(e => new { e.BarId, e.UserId });
        }

    }
}