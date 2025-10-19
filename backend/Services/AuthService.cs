using System;
using backend.UserAuth.Data;
using backend.UserAuth.Models;
using backend.Utils;
using backend.Utils.Errors;

namespace backend.UserAuth.Services
{
    /// <summary>
    /// Handles user registration and login using BCrypt hashing and Result<T> pattern.
    /// </summary>
    public class AuthService
    {
        private readonly UserRepository _repo;

        public AuthService(UserRepository repo)
        {
            _repo = repo;
        }

        /// <summary>
        /// Registers a new user.
        /// Returns Result<UserModel> with success or failure.
        /// </summary>
        public Result<UserModel> Register(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return Result<UserModel>.Failure(StandardErrors.InvalidInput);

            var existingUser = _repo.GetUserByUsername(username);
            if (existingUser != null)
                return Result<UserModel>.Failure("USERNAME_TAKEN", "This username is already taken.");

            try
            {
                var hashedPassword = PasswordHelper.HashPassword(password);

                var newUser = new UserModel(
                    Guid.NewGuid().ToString(),
                    username,
                    hashedPassword,
                    salt: string.Empty // still here for compatibility with old schema
                );

                _repo.SaveUser(newUser);
                return Result<UserModel>.Success(newUser);
            }
            catch (Exception ex)
            {
                // Generic DB error for now
                return Result<UserModel>.Failure("DB_ERROR", $"Database error: {ex.Message}");
            }
        }

        /// <summary>
        /// Logs in a user and validates their credentials.
        /// Returns Result<UserModel> with success or failure.
        /// </summary>
        public Result<UserModel> Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return Result<UserModel>.Failure(StandardErrors.InvalidInput);

            var user = _repo.GetUserByUsername(username);
            if (user == null)
                return Result<UserModel>.Failure(StandardErrors.NotFound);

            var validPassword = PasswordHelper.VerifyPassword(password, user.PasswordHash);
            if (!validPassword)
                return Result<UserModel>.Failure(StandardErrors.Unauthorized);

            return Result<UserModel>.Success(user);
        }
    }
}
