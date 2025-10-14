namespace backend.UserAuth.Models
{
    public class UserModel
    {
        public string Id { get; set; }           // Unique user ID
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string Salt { get; set; }

        public UserModel(string id, string username, string hash, string salt)
        {
            Id = id;
            Username = username;
            PasswordHash = hash;
            Salt = salt;
        }
    }
}
