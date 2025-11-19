using System;
using backend.Data;
using backend.Models;
using backend.Utils.Errors;
using backend.Repositories.Interfaces;
using backend.Common;
using backend.Utils;
using backend.Services.Interfaces;
using Microsoft.Extensions.Logging;
using backend.Exceptions;
namespace backend.Services
{
    // Handles user registration and login using BCrypt hashing and Result<T> pattern
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _repo;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IUserRepository repo, ILogger<AuthService> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        // Registers a new user
        public Result<User> Register(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return Result<User>.Failure(StandardErrors.InvalidInput);

            try
            {
                var hashedPassword = PasswordHelper.HashPassword(password);

                var newUser = new User(
                    Guid.NewGuid(),
                    username,
                    hashedPassword,
                    salt: string.Empty
                );

                var saveResult = _repo.SaveUser(newUser);
                // If repo returned failure (e.g. DB error), forward it:
                if (saveResult.IsFailure)
                    return Result<User>.Failure(saveResult.Error!);

                return Result<User>.Success(saveResult.Value!);
            }
            catch (DuplicateUserException dex)
            {
                // meaningful handling: log and return a typed failure for callers
                _logger.LogWarning(dex, "Registration failed - duplicate username {Username}", username);
                return Result<User>.Failure("DUPLICATE_USER", dex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during registration for {Username}", username);
                return Result<User>.Failure("DB_ERROR", $"Database error: {ex.Message}");
            }
        }

        // Logs in a user and validates their credentials
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
