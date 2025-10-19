using backend.UserAuth.Services;
using backend.UserAuth.Data;
using Microsoft.Extensions.Logging;

namespace backend.UserAuth.Controllers
{
    /// <summary>
    /// Controller for handling user authentication.
    /// Uses ILogger and DI for UserRepository.
    /// </summary>
    public class AuthController
    {
        private readonly AuthService _authService;
        private readonly UserRepository _userRepository;
        private readonly ILogger<AuthController> _logger;

        // Inject dependencies via constructor
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
    }
}
