using System;
using backend.Data;
using backend.Models;
using backend.Utils.Errors;
using backend.Repositories.Interfaces;
using backend.Common;
using backend.Utils;
using backend.Services.Interfaces;
namespace backend.Services
{
    /// <summary>
    /// Handles user registration and login using BCrypt hashing and Result<T> pattern.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _repo;

        public AuthService(IUserRepository repo)
        {
            _repo = repo;
        }

        /// <summary>
        /// Registers a new user.
        /// Returns Result<User> with success or failure.
        /// </summary>
        public Result<User> Register(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return Result<User>.Failure(StandardErrors.InvalidInput);

            var existingUserResult = _repo.GetUserByUsername(username);
            if (existingUserResult.IsSuccess && existingUserResult.Value != null)
                return Result<User>.Failure("USERNAME_TAKEN", "This username is already taken.");

            try
            {
                var hashedPassword = PasswordHelper.HashPassword(password);

                var newUser = new User(
                    Guid.NewGuid(),
                    username,
                    hashedPassword,
                    salt: string.Empty // still here for schema compatibility
                );

                var saveResult = _repo.SaveUser(newUser);
                if (saveResult.IsFailure)
                    return Result<User>.Failure(saveResult.Error!);

                // ✅ FIX: unwrap saveResult.Value instead of passing saveResult itself
                return Result<User>.Success(saveResult.Value!);
            }
            catch (Exception ex)
            {
                // Generic DB error for now
                return Result<User>.Failure("DB_ERROR", $"Database error: {ex.Message}");
            }
        }

        /// <summary>
        /// Logs in a user and validates their credentials.
        /// Returns Result<User> with success or failure.
        /// </summary>
        public Result<User> Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return Result<User>.Failure(StandardErrors.InvalidInput);

            var userResult = _repo.GetUserByUsername(username);
            if (userResult.IsFailure || userResult.Value == null)
                return Result<User>.Failure(StandardErrors.NotFound);

            var user = userResult.Value;

            bool validPassword = PasswordHelper.VerifyPassword(password, user.PasswordHash);
            if (!validPassword)
                return Result<User>.Failure(StandardErrors.Unauthorized);

            return Result<User>.Success(user);
        }
    }
}
