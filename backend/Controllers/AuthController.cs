using backend.UserAuth.Services;
using backend.UserAuth.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Data.SqlClient;

namespace backend.UserAuth.Controllers
{
    /// <summary>
    /// Controller for handling user authentication.
    /// Uses ILogger, session tokens, and DI.
    /// </summary>
    public class AuthController
    {
        private readonly AuthService _authService;
        private readonly UserRepository _userRepository;
        private readonly ILogger<AuthController> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthController(
            AuthService authService,
            UserRepository userRepository,
            ILogger<AuthController> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _authService = authService;
            _userRepository = userRepository;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Registers a new user.
        /// Logs detailed info and handles duplicate usernames.
        /// </summary>
        public void Register(string username, string password)
        {
            _logger.LogInformation("Attempting to register new user: {Username}", username);

            try
            {
                bool success = _authService.Register(username, password);

                if (success)
                {
                    _logger.LogInformation("Registration successful for user: {Username}", username);

                    var user = _userRepository.GetUserByUsername(username);
                    if (user != null)
                        _logger.LogDebug("New user created: {@User}", user);
                }
                else
                {
                    _logger.LogWarning(
                        "Registration failed for user: {Username} (invalid input or other reason)",
                        username
                    );
                }
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                _logger.LogWarning("Registration failed: username '{Username}' is already taken.", username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during registration for user: {Username}", username);
            }
        }

        /// <summary>
        /// Logs in a user and stores their ID in session.
        /// </summary>
        public void Login(string username, string password)
        {
            _logger.LogInformation("Attempting to log in user: {Username}", username);

            try
            {
                bool success = _authService.Login(username, password);

                if (success)
                {
                    _logger.LogInformation("Login successful for user: {Username}", username);

                    var user = _userRepository.GetUserByUsername(username);
                    if (user != null)
                    {
                        _logger.LogDebug("User authenticated: {@User}", user);

                        // Store user ID in session
                        _httpContextAccessor.HttpContext.Session.SetString("UserId", user.Id.ToString());
                        _logger.LogDebug("User ID {UserId} stored in session", user.Id);
                    }
                }
                else
                {
                    _logger.LogWarning("Login failed for user: {Username}", username);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during login for user: {Username}", username);
            }
        }

        /// <summary>
        /// Logs out the current user by clearing session.
        /// </summary>
        public void Logout()
        {
            var session = _httpContextAccessor.HttpContext.Session;
            session.Remove("UserId");
            _logger.LogInformation("User logged out, session cleared");
        }

        /// <summary>
        /// Returns the currently logged-in user's ID from session.
        /// </summary>
        public string? GetCurrentUserId()
        {
            return _httpContextAccessor.HttpContext.Session.GetString("UserId");
        }
    }
}
