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

            // Map Credits struct to a single integer column
            var creditsConverter = new ValueConverter<Credits, int>(
                c => c.Total,        // struct -> int
                v => new Credits(v)  // int -> struct
            );

            modelBuilder.Entity<User>()
                .Property(u => u.Credits)
                .HasConversion(creditsConverter)
                .HasColumnName("CreditsTotal") // the actual column name in DB
                .IsRequired();
        }
    }
}
