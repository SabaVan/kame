using System;
using System.Collections.Generic;
using System.Linq;
using backend.UserAuth.Models;
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
        public List<UserModel> GetAllUsers()
        {
            try
            {
                return _context.Users.AsNoTracking().ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve users from the database.");
                return new List<UserModel>();
            }
        }

        /// <summary>
        /// Save a new user to the database.
        /// </summary>
        public void SaveUser(UserModel user)
        {
            try
            {
                _context.Users.Add(user);
                _context.SaveChanges();
                _logger.LogInformation("User {Username} saved successfully.", user.Username);
            }
            catch (DbUpdateException ex) when (ex.InnerException != null)
            {
                _logger.LogWarning(ex, "Failed to save user {Username} - possible duplicate.", user.Username);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error saving user {Username}.", user.Username);
                throw;
            }
        }

        /// <summary>
        /// Get a user by username.
        /// </summary>
        public UserModel? GetUserByUsername(string username)
        {
            try
            {
                return _context.Users
                    .AsNoTracking()
                    .FirstOrDefault(u => u.Username == username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve user by username: {Username}", username);
                return null;
            }
        }

        /// <summary>
        /// Get a user by ID.
        /// </summary>
        public UserModel? GetUserById(string id)
        {
            try
            {
                return _context.Users
                    .AsNoTracking()
                    .FirstOrDefault(u => u.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve user by ID: {UserId}", id);
                return null;
            }
        }

        /// <summary>
        /// Check if a username already exists.
        /// </summary>
        public bool UsernameExists(string username)
        {
            try
            {
                return _context.Users.Any(u => u.Username == username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check if username exists: {Username}", username);
                return false;
            }
        }
    }
}
