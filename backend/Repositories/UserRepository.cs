using System;
using System.Collections.Generic;
using System.Linq;
using backend.UserAuth.Models;
using backend.Utils;
using backend.Utils.Errors;
using backend.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace backend.UserAuth.Data
{
    public class UserRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UserRepository> _logger;

        // Inject DbContext and ILogger via DI
        public UserRepository(AppDbContext context, ILogger<UserRepository> logger)
        {
            _context = context; // Scoped DbContext reused automatically per request
            _logger = logger;
        }

        /// <summary>
        /// Get all users from the database.
        /// </summary>
        public Result<List<UserModel>> GetAllUsers()
        {
            try
            {
                var users = _context.Users.AsNoTracking().ToList();
                return Result<List<UserModel>>.Success(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve users from the database.");
                return Result<List<UserModel>>.Failure("DB_ERROR", "Database error while retrieving users.");
            }
        }

        /// <summary>
        /// Save a new user to the database.
        /// </summary>
        public Result<UserModel> SaveUser(UserModel user)
        {
            try
            {
                _context.Users.Add(user);
                _context.SaveChanges();

                _logger.LogInformation("User {Username} saved successfully.", user.Username);
                return Result<UserModel>.Success(user);
            }
            catch (DbUpdateException ex) when (ex.InnerException != null)
            {
                _logger.LogWarning(ex, "Failed to save user {Username} - possible duplicate.", user.Username);
                return Result<UserModel>.Failure("DUPLICATE_USER", $"Username '{user.Username}' is already taken.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error saving user {Username}.", user.Username);
                return Result<UserModel>.Failure("DB_ERROR", "Unexpected database error occurred while saving user.");
            }
        }

        /// <summary>
        /// Get a user by username.
        /// </summary>
        public Result<UserModel> GetUserByUsername(string username)
        {
            try
            {
                var user = _context.Users
                    .AsNoTracking()
                    .FirstOrDefault(u => u.Username == username);

                if (user == null)
                {
                    _logger.LogWarning("User not found with username: {Username}", username);
                    return Result<UserModel>.Failure(StandardErrors.NotFound);
                }

                return Result<UserModel>.Success(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve user by username: {Username}", username);
                return Result<UserModel>.Failure("DB_ERROR", "Database error occurred while fetching user by username.");
            }
        }

        /// <summary>
        /// Get a user by ID.
        /// </summary>
        public Result<UserModel> GetUserById(string id)
        {
            try
            {
                var user = _context.Users
                    .AsNoTracking()
                    .FirstOrDefault(u => u.Id == id);

                if (user == null)
                {
                    _logger.LogWarning("User not found with ID: {UserId}", id);
                    return Result<UserModel>.Failure(StandardErrors.NotFound);
                }

                return Result<UserModel>.Success(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve user by ID: {UserId}", id);
                return Result<UserModel>.Failure("DB_ERROR", "Database error occurred while fetching user by ID.");
            }
        }

        /// <summary>
        /// Check if a username already exists.
        /// </summary>
        public Result<bool> UsernameExists(string username)
        {
            try
            {
                bool exists = _context.Users.Any(u => u.Username == username);
                return Result<bool>.Success(exists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check if username exists: {Username}", username);
                return Result<bool>.Failure("DB_ERROR", "Database error occurred while checking username existence.");
            }
        }
    }
}
