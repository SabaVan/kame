using System;
using System.Collections.Generic;
using System.Linq;
using backend.Models;
using backend.Utils.Errors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using backend.Common;
using backend.Data;
using backend.Repositories.Interfaces;

namespace backend.Repositories
{
    public class UserRepository : IUserRepository
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
        public Result<List<User>> GetAllUsers()
        {
            try
            {
                var users = _context.Users.AsNoTracking().ToList();
                return Result<List<User>>.Success(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve users from the database.");
                return Result<List<User>>.Failure("DB_ERROR", "Database error while retrieving users.");
            }
        }

        /// <summary>
        /// Save a new user to the database.
        /// </summary>
        public Result<User> SaveUser(User user)
        {
            try
            {
                _context.Users.Add(user);
                _context.SaveChanges();

                _logger.LogInformation("User {Username} saved successfully.", user.Username);
                return Result<User>.Success(user);
            }
            catch (DbUpdateException ex) when (ex.InnerException != null)
            {
                _logger.LogWarning(ex, "Failed to save user {Username} - possible duplicate.", user.Username);
                return Result<User>.Failure("DUPLICATE_USER", $"Username '{user.Username}' is already taken.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error saving user {Username}.", user.Username);
                return Result<User>.Failure("DB_ERROR", "Unexpected database error occurred while saving user.");
            }
        }

        /// <summary>
        /// Update user.
        /// </summary>
        public Result<User> UpdateUser(User user)
        {
            try
            {
                var existingUser = _context.Users
                    .AsNoTracking()
                    .FirstOrDefault(u => u.Id == user.Id);

                if (existingUser == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found.", user.Id);
                    return Result<User>.Failure("USER_NOT_FOUND", $"User with ID '{user.Id}' does not exist.");
                }

                // Update properties
                existingUser.Username = user.Username;
                existingUser.Credits = user.Credits;

                _context.SaveChanges();

                _logger.LogInformation("User {Username} updated successfully.", user.Username);
                return Result<User>.Success(existingUser);
            }
            catch (DbUpdateException ex) when (ex.InnerException != null)
            {
                _logger.LogWarning(ex, "Failed to update user {Username} - possible duplicate.", user.Username);
                return Result<User>.Failure("DUPLICATE_USER", $"Username '{user.Username}' is already taken.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating user {Username}.", user.Username);
                return Result<User>.Failure("DB_ERROR", "Unexpected database error occurred while updating user.");
            }
        }

        /// <summary>
        /// Get a user by username.
        /// </summary>
        public Result<User> GetUserByUsername(string username)
        {
            try
            {
                var user = _context.Users
                    .AsNoTracking()
                    .FirstOrDefault(u => u.Username == username);

                if (user == null)
                {
                    _logger.LogWarning("User not found with username: {Username}", username);
                    return Result<User>.Failure(StandardErrors.NotFound);
                }

                return Result<User>.Success(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve user by username: {Username}", username);
                return Result<User>.Failure("DB_ERROR", "Database error occurred while fetching user by username.");
            }
        }

        /// <summary>
        /// Get a user by ID.
        /// </summary>
        public Result<User> GetUserById(Guid id)
        {
            try
            {
                var user = _context.Users
                    .AsNoTracking()
                    .FirstOrDefault(u => u.Id == id);

                if (user == null)
                {
                    _logger.LogWarning("User not found with ID: {UserId}", id);
                    return Result<User>.Failure(StandardErrors.NotFound);
                }

                return Result<User>.Success(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve user by ID: {UserId}", id);
                return Result<User>.Failure("DB_ERROR", "Database error occurred while fetching user by ID.");
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
