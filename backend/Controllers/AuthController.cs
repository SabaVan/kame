using backend.Services.Interfaces;
using backend.Repositories.Interfaces;
using backend.Common;
using backend.Utils.Errors;
using Microsoft.AspNetCore.Mvc;


namespace backend.Controllers
{
    /// <summary>
    /// Controller for handling user authentication.
    /// Uses ILogger, session tokens, and DI.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IUserRepository _userRepository;
        private readonly IBarUserEntryRepository _barUserEntries;
        private readonly ILogger<AuthController> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthController(
            IAuthService authService,
            IUserRepository userRepository,
            ILogger<AuthController> logger,
            IBarUserEntryRepository barUserEntries,
            IHttpContextAccessor httpContextAccessor)
        {
            _barUserEntries = barUserEntries;
            _authService = authService;
            _userRepository = userRepository;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        // Internal helper for logout logic
        private async Task<Result<string>> LogoutInternalAsync()
        {
            try
            {
                var context = _httpContextAccessor.HttpContext;
                if (context == null)
                    return Result<string>.Failure("NO_HTTP_CONTEXT", "No HTTP context available.");

                var session = context.Session;
                if (session == null)
                    return Result<string>.Failure("NO_SESSION", "Session not available.");

                var userIdString = session.GetString("UserId");
                if (!Guid.TryParse(userIdString, out var userId))
                {
                    session.Clear(); // still clear session even if no userId
                    return Result<string>.Success("Logged out successfully (no active user).");
                }

                // Remove user from all bars
                var bars = await _barUserEntries.GetBarsForUserAsync(userId);
                foreach (var bar in bars)
                {
                    await _barUserEntries.RemoveEntryAsync(bar.Id, userId);
                }

                await _barUserEntries.SaveChangesAsync();

                // Clear session
                session.Clear();

                _logger.LogInformation("User {UserId} logged out, removed from bars, session cleared", userId);
                return Result<string>.Success("Logged out successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during logout.");
                return Result<string>.Failure("SESSION_ERROR", "An error occurred while clearing session.");
            }
        }

        /// <summary>
        /// Registers a new user and automatically logs them in.
        /// Returns Result<User> with session set.
        /// </summary>
        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequest request)
        {
            // Attempt to register user
            var result = _authService.Register(request.Username, request.Password);
            if (result.IsFailure)
            {
                _logger.LogWarning("Registration failed for {Username}: {Error}", request.Username, result.Error?.Message);
                return BadRequest(result.Error);
            }

            var user = result.Value!;
            _logger.LogInformation("User {Username} registered successfully with ID {UserId}", user.Username, user.Id);

            // Automatically log in the new user (set session)
            var context = _httpContextAccessor.HttpContext;
            if (context == null)
            {
                _logger.LogWarning("No active HttpContext found while registering user {Username}", request.Username);
                return BadRequest(new { Code = "NO_HTTP_CONTEXT", Message = "No active HTTP context available." });
            }

            var session = context.Session;
            session.SetString("UserId", user.Id.ToString());
            _logger.LogDebug("User ID {UserId} stored in session after registration", user.Id);

            return Ok(user);
        }


        /// <summary>
        /// Logs in a user and stores their ID in session.
        /// Returns Result<User> on success.
        /// </summary>
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            var result = _authService.Login(request.Username, request.Password);
            if (result.IsFailure)
            {
                _logger.LogWarning("Login failed for {Username}: {Error}", request.Username, result.Error?.Message);
                return BadRequest(result.Error);
            }

            var user = result.Value!;

            var context = _httpContextAccessor.HttpContext;
            if (context == null)
            {
                _logger.LogWarning("No active HttpContext found while logging in user {Username}", request.Username);
                return BadRequest(new { Code = "NO_HTTP_CONTEXT", Message = "No active HTTP context available." });
            }

            var session = context.Session;
            session.SetString("UserId", user.Id.ToString());
            _logger.LogDebug("User ID {UserId} stored in session", user.Id);

            return Ok(user);
        }

        /// <summary>
        /// Logs out the current user by clearing session.
        /// Returns Result<string> for consistency.
        /// </summary>
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var result = await LogoutInternalAsync();
            if (result.IsFailure)
                return BadRequest(result.Error);

            return Ok(result.Value);
        }

        /// <summary>
        /// Returns the currently logged-in user's ID from session.
        /// </summary>
        [HttpGet("current-user-id")]
        public IActionResult GetCurrentUserId()
        {
            try
            {
                var context = _httpContextAccessor.HttpContext;
                if (context == null)
                {
                    _logger.LogWarning("No active HttpContext when fetching current user ID.");
                    return BadRequest(new { Code = "NO_HTTP_CONTEXT", Message = "No active HTTP context available." });
                }

                var session = context.Session;
                var userId = session?.GetString("UserId");

                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("No user is currently logged in.");
                    return Unauthorized(StandardErrors.Unauthorized);
                }

                return Ok(new { success = true, userId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching current user ID from session.");
                return BadRequest(new { Code = "SESSION_ERROR", Message = "Failed to retrieve user from session." });
            }
        }
    }

    // Request models
    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
