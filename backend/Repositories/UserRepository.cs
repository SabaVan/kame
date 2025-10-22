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
        public async Task<List<User>> GetAllAsync()
        {
            return await _context.Users.ToListAsync();
        }

        /// <summary>
        /// Save a new user to the database.
        /// </summary>
        public async Task<Result<User>> AddAsync(User user)
        {
            // get user, check if exists
            var existing = await _context.Users.FindAsync(user.Id);
            if (existing != null)
            {
                _logger.LogWarning("User with ID {UserId} already exists.", user.Id);
                return Result<User>.Failure("USER_EXISTS", "User with the same ID already exists.");
            }

            await _context.Users.AddAsync(user);
            return Result<User>.Success(user);
        }

        /// <summary>
        /// Update a user in the database.
        /// </summary>
        public async Task<Result<User>> UpdateAsync(User user)
        {
            var existing = await _context.Users.FindAsync(user.Id);

            if (existing == null)
            {
                _logger.LogWarning("User with ID {UserId} does not exist.", user.Id);
                return Result<User>.Failure(StandardErrors.NotFound);
            }

            existing.Username = user.Username;
            existing.Credits = user.Credits;
            _context.Users.Update(existing);
            await _context.SaveChangesAsync();

            return Result<User>.Success(existing);
        }

        /// <summary>
        /// Get a user by username.
        /// </summary>
        public async Task<Result<User>> GetByNameAsync(string username)
        {
            var existing = await _context.Users.FindAsync(username);

            if (existing == null)
            {
                _logger.LogWarning("User not found with username: {Username}", username);
                return Result<User>.Failure(StandardErrors.NotFound);
            }

            return Result<User>.Success(existing);
        }

        /// <summary>
        /// Get a user by ID.
        /// </summary>
        public async Task<Result<User>> GetByIdAsync(Guid id)
        {
            var existing = await _context.Users.FindAsync(id);

            if (existing == null)
            {
                _logger.LogWarning("User not found with ID: {UserId}", id);
                return Result<User>.Failure(StandardErrors.NotFound);
            }

            return Result<User>.Success(existing);
        }

        /// <summary>
        /// Check if a username already exists.
        /// </summary>
        public async Task<Result<bool>> UsernameExistsAsync(string username)
        {
            var result = await GetByNameAsync(username);

            if (!result.IsSuccess)
            {
                return Result<bool>.Failure(StandardErrors.NotFound);
            }

            return Result<bool>.Success(true);
        }

        /// <summary>
        /// Delete a user by ID.
        /// </summary>
        public async Task<Result<bool>> DeleteAsync(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return Result<bool>.Failure(StandardErrors.NotFound);
            _context.Users.Remove(user);

            return Result<bool>.Success(true);
        }
    }
}
