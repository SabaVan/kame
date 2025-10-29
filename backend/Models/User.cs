using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using backend.Models;

namespace backend.Models
{
    [Table("Users")]
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;
        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public Credits Credits { get; set; } = new Credits(initialAmount: 100);

        // Still here for backward compatibility (but unused if using BCrypt)
        public string Salt { get; set; } = string.Empty;

        // Optional parameterized constructor for manual creation
        public User() { }

        public User(Guid id, string username, string passwordHash, string salt)
        {
            Id = id;
            Username = username;
            PasswordHash = passwordHash;
            Salt = salt;
        }
    }
}