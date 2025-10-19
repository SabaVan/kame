using backend.UserAuth.Services;
using backend.UserAuth.Data;
using Microsoft.Extensions.Logging;
using System.Data.SqlClient; // For SqlException

namespace backend.UserAuth.Controllers
{
    public class AuthController
    {
        private readonly AuthService _authService;
        private readonly UserRepository _userRepository;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            AuthService authService,
            UserRepository userRepository,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _userRepository = userRepository;
            _logger = logger;
        }

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
                    {
                        _logger.LogDebug("New user created: {@User}", user);
                    }
                }
                else
                {
                    _logger.LogWarning(
                        "Registration failed for user: {Username} (invalid input or other reason)",
                        username
                    );
                }
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601) // SQL Server duplicate key
            {
                // These numbers correspond to "unique constraint violation"
                _logger.LogWarning("Registration failed: username '{Username}' is already taken.", username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during registration for user: {Username}", username);
            }
        }

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
    }
}
