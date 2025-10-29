using Microsoft.AspNetCore.Mvc;
using backend.Services.Interfaces;
using backend.Repositories.Interfaces;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UsersController(
            IUserRepository userRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _userRepository = userRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpGet("profile")]
        public IActionResult GetProfile()
        {
            var userId = _httpContextAccessor.HttpContext?.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "Not logged in" });
            }

            var userResult = _userRepository.GetUserById(Guid.Parse(userId));
            if (userResult.IsFailure || userResult.Value == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var user = userResult.Value;
            return Ok(new
            {
                username = user.Username,
                credits = user.Credits // Make sure your User model has a Credits property
            });
        }
    }
}