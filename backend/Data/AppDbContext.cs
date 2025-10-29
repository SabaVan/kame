using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace backend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        // DbSets
        public DbSet<Bar> Bars { get; set; }
        public DbSet<BarUserEntry> BarUserEntries { get; set; }
        public DbSet<Song> Songs { get; set; }
        public DbSet<PlaylistSong> PlaylistSongs { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Playlist> Playlists { get; set; }
        public DbSet<BarPlaylistEntry> BarPlaylistEntries { get; set; }
        public DbSet<CreditTransaction> CreditTransactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Composite keys
            modelBuilder.Entity<BarUserEntry>()
                .HasKey(e => new { e.BarId, e.UserId });

            modelBuilder.Entity<BarPlaylistEntry>()
                .HasKey(e => new { e.BarId, e.PlaylistId });

            // PlaylistSong relationships
            modelBuilder.Entity<PlaylistSong>()
                .HasOne(ps => ps.Song)
                .WithMany()
                .HasForeignKey(ps => ps.SongId);

            modelBuilder.Entity<PlaylistSong>()
                .HasOne<Playlist>()
                .WithMany(p => p.Songs)
                .HasForeignKey(ps => ps.PlaylistId);

            // CreditTransaction relationships
            modelBuilder.Entity<CreditTransaction>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(ct => ct.UserId);

            modelBuilder.Entity<CreditTransaction>()
                .HasOne<Bar>()
                .WithMany()
                .HasForeignKey(ct => ct.BarId);

            modelBuilder.Entity<User>(entity =>
            {
                entity.OwnsOne(u => u.Credits, credits =>
        {
            credits.Property(c => c.Total)
           .HasColumnName("CreditsTotal")
           .HasDefaultValue(0);
        });
            });
        }
    }
}
