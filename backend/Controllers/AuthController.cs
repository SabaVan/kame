using System;
using backend.UserAuth.Services;
using backend.UserAuth.Data;
using Microsoft.Extensions.Logging;

namespace backend.UserAuth.Controllers
{
    /// <summary>
    /// Controller for handling user authentication.
    /// Logging replaces Console output.
    /// </summary>
    public class AuthController
    {
        private readonly AuthService _authService = new AuthService();
        private readonly ILogger<AuthController> _logger;

        // Constructor injection for ILogger
        public AuthController(ILogger<AuthController> logger)
        {
            _logger = logger;
        }

        public void Register(string username, string password)
        {
            _logger.LogInformation("Attempting to register new user: {Username}", username);

            bool success = _authService.Register(username, password);

            if (success)
            {
                _logger.LogInformation("Registration successful for user: {Username}", username);

                var repo = new UserRepository();
                var user = repo.GetUserByUsername(username);
                if (user != null)
                {
                    _logger.LogDebug("New user created: {@User}", user);
                }
            }
            else
            {
                _logger.LogWarning(
                    "Registration failed for user: {Username} (user may exist or invalid input)", 
                    username
                );
            }
        }

        public void Login(string username, string password)
        {
            _logger.LogInformation("Attempting to log in user: {Username}", username);

            bool success = _authService.Login(username, password);

            if (success)
            {
                _logger.LogInformation("Login successful for user: {Username}", username);

                var repo = new UserRepository();
                var user = repo.GetUserByUsername(username);
                if (user != null)
                {
                    _logger.LogDebug("User authenticated: {@User}", user);
                }
            }
            else
            {
                _logger.LogWarning("Login failed for user: {Username}", username);
            }
        }
    }
}
