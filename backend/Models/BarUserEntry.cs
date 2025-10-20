using System.ComponentModel.DataAnnotations.Schema;
using backend.Models;
namespace backend.Models
{
    public class BarUserEntry
    {
        public Guid BarId { get; set; }
        public Guid UserId { get; set; }
        public DateTime EnteredAt { get; set; } = DateTime.Now;
        public Bar Bar { get; set; } = null!;  // navigation
        public User User { get; set; } = null!; // navigation
                                                // Parameterless constructor required by EF Core
        public BarUserEntry() { }
        public BarUserEntry(Bar bar, User user)
        {
            BarId = bar.Id;
            UserId = user.Id;
            Bar = bar;
            User = user;
        }
    }
}