using System;
using backend.UserAuth.Services;
using backend.UserAuth.Data;

namespace backend.UserAuth.Controllers
{
    /// <summary>
    /// Controller for handling user authentication.
    /// No interactive UI, only debug console output.
    /// </summary>
    public class AuthController
    {
        private readonly AuthService _authService = new AuthService();

        public void Register(string username, string password)
        {
            Console.WriteLine("Attempting to register new user...");

            bool success = _authService.Register(username, password);

            if (success)
            {
                Console.WriteLine("Registration successful.");
                var repo = new UserRepository();
                var user = repo.GetUserByUsername(username);
                if (user != null)
                {
                    Console.WriteLine("[DEBUG] New user created:");
                    Console.WriteLine($" - ID: {user.Id}");
                    Console.WriteLine($" - Username: {user.Username}");
                    Console.WriteLine($" - PasswordHash: {user.PasswordHash}");
                    Console.WriteLine($" - Salt: {user.Salt}");
                }
            }
            else
            {
                Console.WriteLine("Registration failed (user may exist or invalid input).");
            }
        }

        public void Login(string username, string password)
        {
            Console.WriteLine($"Attempting to log in user '{username}'...");

            bool success = _authService.Login(username, password);

            if (success)
            {
                Console.WriteLine($"Login successful for user: {username}");
                var repo = new UserRepository();
                var user = repo.GetUserByUsername(username);
                if (user != null)
                {
                    Console.WriteLine("[DEBUG] User authenticated:");
                    Console.WriteLine($" - ID: {user.Id}");
                    Console.WriteLine($" - Username: {user.Username}");
                    Console.WriteLine($" - PasswordHash: {user.PasswordHash}");
                    Console.WriteLine($" - Salt: {user.Salt}");
                }
            }
            else
            {
                Console.WriteLine($"Login failed for user: {username}");
            }
        }
    }
}
