using backend.UserAuth.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.UserAuth.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<UserModel> Users { get; set; }
    }
}
