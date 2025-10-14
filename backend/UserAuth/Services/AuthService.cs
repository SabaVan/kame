using System;
using backend.UserAuth.Data;
using backend.UserAuth.Models;

namespace backend.UserAuth.Services
{
    /// <summary>
    /// Handles user registration and login.
    /// </summary>
    public class AuthService
    {
        private readonly UserRepository _repo = new UserRepository();

        public bool Register(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return false;

            var existingUser = _repo.GetUserByUsername(username);
            if (existingUser != null)
                return false;

            PasswordHelper.CreatePasswordHash(password, out byte[] hash, out byte[] salt);
            var newUser = new UserModel(
                Guid.NewGuid().ToString(),                   // Generate unique ID
                username,
                Convert.ToBase64String(hash),
                Convert.ToBase64String(salt)
            );

            _repo.SaveUser(newUser);
            return true;
        }

        public bool Login(string username, string password)
        {
            var user = _repo.GetUserByUsername(username);
            if (user == null) return false;

            var storedHash = Convert.FromBase64String(user.PasswordHash);
            var storedSalt = Convert.FromBase64String(user.Salt);
            return PasswordHelper.VerifyPassword(password, storedHash, storedSalt);
        }
    }
}
