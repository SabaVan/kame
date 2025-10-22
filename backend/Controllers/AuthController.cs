using backend.Services.Interfaces;
using backend.Repositories.Interfaces;
using backend.Common;
using backend.Utils.Errors;
using backend.Models;
namespace backend.Controllers
{
    /// <summary>
    /// Controller for handling user authentication.
    /// Uses ILogger, session tokens, and DI.
    /// </summary>
    public class AuthController
    {
        private readonly IAuthService _authService;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<AuthController> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthController(
            IAuthService authService,
            IUserRepository userRepository,
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
        /// Returns Result<User> with detailed status.
        /// </summary>
        public Result<User> Register(string username, string password)
        {
            _logger.LogInformation("Attempting to register new user: {Username}", username);

            var result = _authService.Register(username, password);
            if (result.IsFailure)
            {
                _logger.LogWarning("Registration failed for {Username}: {Error}", username, result.Error?.Message);
                return Result<User>.Failure(result.Error ?? StandardErrors.InvalidInput);
            }

            var userResult = _userRepository.GetUserByUsername(username);
            if (userResult.IsFailure)
            {
                _logger.LogWarning("Registration succeeded, but failed to fetch created user: {Username}", username);
                return Result<User>.Failure(userResult.Error ?? StandardErrors.NotFound);
            }

            _logger.LogInformation("Registration successful for user: {Username}", username);
            _logger.LogDebug("New user created: {@User}", userResult.Value);

            return Result<User>.Success(userResult.Value!);
        }

        /// <summary>
        /// Logs in a user and stores their ID in session.
        /// Returns Result<User> on success.
        /// </summary>
        public Result<User> Login(string username, string password)
        {
            _logger.LogInformation("Attempting to log in user: {Username}", username);

            var result = _authService.Login(username, password);
            if (result.IsFailure)
            {
                _logger.LogWarning("Login failed for {Username}: {Error}", username, result.Error?.Message);
                return Result<User>.Failure(result.Error ?? StandardErrors.Unauthorized);
            }

            var userResult = _userRepository.GetUserByUsername(username);
            if (userResult.IsFailure)
            {
                _logger.LogWarning("Login succeeded but failed to fetch user from DB: {Username}", username);
                return Result<User>.Failure(userResult.Error ?? StandardErrors.NotFound);
            }

            var user = userResult.Value!;
            _logger.LogInformation("Login successful for user: {Username}", username);
            _logger.LogDebug("User authenticated: {@User}", user);

            var context = _httpContextAccessor.HttpContext;
            if (context == null)
            {
                _logger.LogWarning("No active HttpContext found while logging in user {Username}", username);
                return Result<User>.Failure("NO_HTTP_CONTEXT", "No active HTTP context available.");
            }

            // Ensure session is available
            var session = context.Session;
            if (session == null)
            {
                _logger.LogWarning("Session was null while logging in user {Username}", username);
                return Result<User>.Failure("NO_SESSION", "Session not available.");
            }

            session.SetString("UserId", user.Id.ToString());
            _logger.LogDebug("User ID {UserId} stored in session", user.Id);

            return Result<User>.Success(user);
        }

        /// <summary>
        /// Logs out the current user by clearing session.
        /// Returns Result<string> for consistency.
        /// </summary>
        public Result<string> Logout()
        {
            try
            {
                var context = _httpContextAccessor.HttpContext;
                if (context == null)
                {
                    _logger.LogWarning("HttpContext was null during logout.");
                    return Result<string>.Failure("NO_HTTP_CONTEXT", "No HTTP context available.");
                }

                var session = context.Session;
                if (session == null)
                {
                    _logger.LogWarning("Session was null during logout.");
                    return Result<string>.Failure("NO_SESSION", "Session not available.");
                }

                session.Remove("UserId");
                _logger.LogInformation("User logged out, session cleared");

                return Result<string>.Success("Logged out successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during logout.");
                return Result<string>.Failure("SESSION_ERROR", "An error occurred while clearing session.");
            }
        }

        /// <summary>
        /// Returns the currently logged-in user's ID from session.
        /// </summary>
        public Result<string> GetCurrentUserId()
        {
            try
            {
                var context = _httpContextAccessor.HttpContext;
                if (context == null)
                {
                    _logger.LogWarning("No active HttpContext when fetching current user ID.");
                    return Result<string>.Failure("NO_HTTP_CONTEXT", "No active HTTP context available.");
                }

                var session = context.Session;
                if (session == null)
                {
                    _logger.LogWarning("No active session found when fetching current user ID.");
                    return Result<string>.Failure("NO_SESSION", "Session not available.");
                }

                var userId = session.GetString("UserId");
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("No user is currently logged in.");
                    return Result<string>.Failure(StandardErrors.Unauthorized);
                }

                return Result<string>.Success(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching current user ID from session.");
                return Result<string>.Failure("SESSION_ERROR", "Failed to retrieve user from session.");
            }
        }
    }
}
