using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.UserAuth.Models
{
    [Table("Users")] // optional: specify table name
    public class UserModel
    {
        [Key]
        public string Id { get; set; }           // Primary key

        [Required]
        [MaxLength(50)]
        public string Username { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        public string Salt { get; set; }

        public UserModel() { } // Parameterless constructor needed for EF

        public UserModel(string id, string username, string hash, string salt)
        {
            Id = id;
            Username = username;
            PasswordHash = hash;
            Salt = salt;
        }
    }
}
