using backend.Services.Interfaces;
using backend.Repositories.Interfaces;
using backend.Common;
using backend.Utils.Errors;
using backend.Models;
using backend.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;

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

        // Internal helper for logout logic
        private Result<string> LogoutInternal()
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
        /// Registers a new user.
        /// Returns Result<User> with detailed status.
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            _logger.LogInformation("Attempting to register new user: {Username}", username);

            var result = await _authService.RegisterAsync(request.Username, request.Password);
            if (result.IsFailure)
            {
                _logger.LogWarning("Registration failed for {Username}: {Error}", request.Username, result.Error?.Message);
                return BadRequest(result.Error);
            }

            var userResult = await _userRepository.GetByNameAsync(username);
            if (userResult.IsFailure)
            {
                _logger.LogWarning("Registration succeeded, but failed to fetch created user: {Username}", username);
                return Result<User>.Failure(userResult.Error ?? StandardErrors.NotFound);
            }

            _logger.LogInformation("Registration successful for user: {Username}", username);
            _logger.LogDebug("New user created: {@User}", userResult.Value);

            return Ok(result.Value);
            
        }

        /// <summary>
        /// Logs in a user and stores their ID in session.
        /// Returns Result<User> on success.
        /// </summary>

        [HttpPost("login")]
        public async Task<IActionResult> Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            _logger.LogInformation("Attempting to log in user: {Username}", username);

            var result = await _authService.LoginAsync(request.Username, request.Password);
            if (result.IsFailure)
            {
                _logger.LogWarning("Login failed for {Username}: {Error}", username, result.Error?.Message);
                return BadRequest(result.Error ?? StandardErrors.Unauthorized);
            }

            var userResult = await _userRepository.GetByNameAsync(username);
            if (userResult.IsFailure)
            {
                _logger.LogWarning("Login succeeded but failed to fetch user from DB: {Username}", username);
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
        public IActionResult Logout()
        {
            var result = LogoutInternal();
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

                return Ok(userId);
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
