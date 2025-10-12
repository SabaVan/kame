using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<Bar> Bars { get; set; }
        //public DbSet<User> Users { get; set; }
        //public DbSet<Song> Songs { get; set; }
        //public DbSet<PlayList> PlayLists { get; set; }
    }
}
