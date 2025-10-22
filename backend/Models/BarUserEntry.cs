using System.ComponentModel.DataAnnotations.Schema;
using backend.Models;
namespace backend.Models
{
    public class BarUserEntry
    {
        public Guid BarId { get; set; }
        public Guid UserId { get; set; }
        public DateTime EnteredAt { get; set; } = DateTime.UtcNow;
        public BarUserEntry() { }
        public BarUserEntry(Bar Bar, User User)
        {
            BarId = Bar.Id;
            UserId = User.Id;
        }
        public BarUserEntry(Guid BarId, Guid UserId)
        {
            this.BarId = BarId;
            this.UserId = UserId;
        }
    }
}