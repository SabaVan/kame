using backend.Models;
using backend.Common;

namespace backend.Repositories.Interfaces
{
    public interface IUserRepository
    {
        /// <summary>
        /// Get all users from the database.
        /// </summary>
        public Result<List<User>> GetAllUsers();

        /// <summary>
        /// Save a new user to the database.
        /// </summary>
        public Result<User> SaveUser(User user);

        /// <summary>
        /// Get a user by username.
        /// </summary>
        public Result<User> GetUserByUsername(string username);

        /// <summary>
        /// Get a user by ID.
        /// </summary>
        public Result<User> GetUserById(Guid id);

        /// <summary>
        /// Check if a username already exists.
        /// </summary>
        public Result<bool> UsernameExists(string username);
    }
}
