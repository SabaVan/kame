using backend.Models;
using backend.Common;

namespace backend.Repositories.Interfaces
{
    public interface IUserRepository
    {
        /// <summary>
        /// Get all users from the database.
        /// </summary>
        public Task<List<User>> GetAllAsync();

        /// <summary>
        /// Save a new user to the database.
        /// </summary>
        public Task<Result<User>> AddAsync(User user);

        /// <summary>
        /// Update a user in the database.
        /// </summary>
        public Task<Result<User>> UpdateAsync(User user);

        /// <summary>
        /// Get a user by username.
        /// </summary>
        public Task<Result<User>> GetByNameAsync(string username);

        /// <summary>
        /// Get a user by ID.
        /// </summary>
        public Task<Result<User>> GetByIdAsync(Guid id);

        /// <summary>
        /// Check if a username already exists.
        /// </summary>
        public Task<Result<bool>> UsernameExistsAsync(string username);

        /// <summary>
        /// Delete a user by ID.
        /// </summary>
        public Task<Result<bool>> DeleteAsync(Guid id);
    }
}
