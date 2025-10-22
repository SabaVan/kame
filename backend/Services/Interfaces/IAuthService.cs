using backend.Models;
using backend.Common;
namespace backend.Services.Interfaces
{
    /// <summary>
    /// Handles user registration and login using BCrypt hashing and Result<T> pattern.
    /// </summary>
    public interface IAuthService
    {

        /// <summary>
        /// Registers a new user.
        /// Returns Result<User> with success or failure.
        /// </summary>
        public Task<Result<User>> RegisterAsync(string username, string password);

        /// <summary>
        /// Logs in a user and validates their credentials.
        /// Returns Result<User> with success or failure.
        /// </summary>
        public Task<Result<User>> LoginAsync(string username, string password);
    }
}
