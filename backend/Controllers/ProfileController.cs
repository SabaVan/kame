using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using backend.Services.Interfaces;
using backend.Repositories.Interfaces;
using backend.Shared.Enums;
using backend.Models;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ICreditService? _creditService;
        private readonly ITransactionRepository? _transactionRepository;

        // Backward-compatible constructor used by some tests. The extra optional parameter
        // prevents the DI container from picking this overload while keeping tests that
        // call the two-argument constructor working.
        public UsersController(
            IUserRepository userRepository,
            IHttpContextAccessor httpContextAccessor,
            object? _ignored = null)
            : this(userRepository, httpContextAccessor, null, null)
        { }

        [Microsoft.Extensions.DependencyInjection.ActivatorUtilitiesConstructor]
        public UsersController(
            IUserRepository userRepository,
            IHttpContextAccessor httpContextAccessor,
            ICreditService? creditService,
            ITransactionRepository? transactionRepository)
        {
            _userRepository = userRepository;
            _httpContextAccessor = httpContextAccessor;
            _creditService = creditService;
            _transactionRepository = transactionRepository;
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

        [HttpPost("claim-daily")]
        public async Task<IActionResult> ClaimDaily()
        {
            var userIdStr = _httpContextAccessor.HttpContext?.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr))
                return Unauthorized(new { message = "Not logged in" });

            var userId = Guid.Parse(userIdStr);
            const int DAILY_AMOUNT = 25;

            if (_creditService == null || _transactionRepository == null)
                return StatusCode(500, new { message = "Credit service unavailable" });

            // 1) Load all transactions
            var logs = await _transactionRepository.GetByUserAsync(userId);

            // 2) Detect last daily claim (case-insensitive + flexible)
            // Normalize CreatedAt to UTC for ordering and accept slightly different reason text.
            var lastDailyItem = logs
                .Where(t =>
                    t.Type == TransactionType.Add &&
                    t.Amount == DAILY_AMOUNT &&
                    t.Reason != null &&
                    (t.Reason.Contains("daily", StringComparison.OrdinalIgnoreCase) ||
                     t.Reason.Contains("daily bonus", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(t.Reason.Trim(), "daily", StringComparison.OrdinalIgnoreCase))
                )
                .Select(t => new
                {
                    Tx = t,
                    CreatedAtUtc = t.CreatedAt.Kind == DateTimeKind.Utc
                        ? t.CreatedAt
                        : (t.CreatedAt.Kind == DateTimeKind.Local
                            ? t.CreatedAt.ToUniversalTime()
                            : DateTime.SpecifyKind(t.CreatedAt, DateTimeKind.Utc))
                })
                .OrderByDescending(x => x.CreatedAtUtc)
                .FirstOrDefault();

            if (lastDailyItem != null)
            {
                var lastClaimUtc = lastDailyItem.CreatedAtUtc;

                var hoursSince = (DateTime.UtcNow - lastClaimUtc).TotalHours;
                if (hoursSince < 24)
                {
                    return BadRequest(new
                    {
                        message = $"Daily credits already claimed. Try again in {24 - hoursSince:F1} hours."
                    });
                }
            }

            // 3) Balance rule
            var balanceResult = _creditService.GetBalance(userId);
            if (balanceResult.IsFailure)
                return NotFound(new { message = "User not found" });

            if (balanceResult.Value >= DAILY_AMOUNT)
            {
                return BadRequest(new
                {
                    message = "Cannot claim when balance is 25 or greater."
                });
            }

            // 4) Add credits
            var addResult = await _creditService.AddCredits(userId, DAILY_AMOUNT, "daily bonus", TransactionType.Add);
            if (addResult.IsFailure)
                return BadRequest(new { message = "Failed to add credits" });

            var newBalance = _creditService.GetBalance(userId);

            return Ok(new
            {
                credits = newBalance.Value,
                message = "Daily credits claimed successfully"
            });
        }


    }
}