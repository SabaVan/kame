using System;
using backend.UserAuth.Data;
using backend.UserAuth.Models;
using backend.Utils;
using backend.Utils.Errors;
using backend.Common;
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

            var existingUserResult = _repo.GetUserByUsername(username);
            if (existingUserResult.IsSuccess && existingUserResult.Value != null)
                return Result<UserModel>.Failure("USERNAME_TAKEN", "This username is already taken.");

            try
            {
                var hashedPassword = PasswordHelper.HashPassword(password);

                var newUser = new UserModel(
                    Guid.NewGuid().ToString(),
                    username,
                    hashedPassword,
                    salt: string.Empty // still here for schema compatibility
                );

                var saveResult = _repo.SaveUser(newUser);
                if (saveResult.IsFailure)
                    return Result<UserModel>.Failure(saveResult.Error!);

                // ✅ FIX: unwrap saveResult.Value instead of passing saveResult itself
                return Result<UserModel>.Success(saveResult.Value!);
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

            var userResult = _repo.GetUserByUsername(username);
            if (userResult.IsFailure || userResult.Value == null)
                return Result<UserModel>.Failure(StandardErrors.NotFound);

            var user = userResult.Value;

            bool validPassword = PasswordHelper.VerifyPassword(password, user.PasswordHash);
            if (!validPassword)
                return Result<UserModel>.Failure(StandardErrors.Unauthorized);

            return Result<UserModel>.Success(user);
        }
    }
}
