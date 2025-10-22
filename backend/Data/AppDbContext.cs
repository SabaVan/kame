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
        public DbSet<Song> Songs { get; set; }
        public DbSet<PlaylistSong> PlaylistSongs { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Playlist> Playlists { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<BarUserEntry>().HasKey(e => new { e.BarId, e.UserId });

            modelBuilder.Entity<PlaylistSong>()
                .HasOne(ps => ps.Song)
                .WithMany()
                .HasForeignKey(ps => ps.SongId);

            modelBuilder.Entity<PlaylistSong>()
                .HasOne<Playlist>()
                .WithMany(p => p.Songs)
                .HasForeignKey(ps => ps.PlaylistId);
        }

    }
}