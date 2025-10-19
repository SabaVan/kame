using BCrypt.Net;

namespace backend.UserAuth.Services
{
    /// <summary>
    /// Provides secure password hashing and verification using BCrypt.
    /// </summary>
    public static class PasswordHelper
    {
        /// <summary>
        /// Creates a BCrypt hash for the given password.
        /// </summary>
        /// <param name="password">Plaintext password</param>
        /// <returns>BCrypt hash string (includes salt)</returns>
        public static string HashPassword(string password)
        {
            // The work factor controls computational cost (default 12)
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        }

        /// <summary>
        /// Verifies a password against a stored BCrypt hash.
        /// </summary>
        /// <param name="password">Plaintext password</param>
        /// <param name="storedHash">Stored BCrypt hash from database</param>
        /// <returns>True if password matches, false otherwise</returns>
        public static bool VerifyPassword(string password, string storedHash)
        {
            return BCrypt.Net.BCrypt.Verify(password, storedHash);
        }
    }
}
