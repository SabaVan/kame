using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.UserAuth.Models
{
    [Table("Users")]
    public class UserModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string Id { get; set; } = Guid.NewGuid().ToString(); // ✅ Non-null default

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty; // ✅ Non-null default

        [Required]
        public string PasswordHash { get; set; } = string.Empty; // ✅ Non-null default

        // Still here for backward compatibility (but unused if using BCrypt)
        public string Salt { get; set; } = string.Empty;

        // Optional parameterized constructor for manual creation
        public UserModel() { }

        public UserModel(string id, string username, string passwordHash, string salt)
        {
            Id = id;
            Username = username;
            PasswordHash = passwordHash;
            Salt = salt;
        }
    }
}
