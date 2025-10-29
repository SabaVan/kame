using AutoMapper;
using backend.Utils.Errors;
using backend.Hubs;
using backend.Models;
using backend.Shared.DTOs;
using backend.Repositories.Interfaces;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using backend.Utils;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BarController : ControllerBase
    {
        private readonly IBarRepository _bars;
        private readonly IBarService _barService;
        private readonly IHubContext<BarHub> _barHub;
        private readonly IMapper _mapper;
        private readonly IBarUserEntryRepository _barUserEntries;


        public BarController(
            IBarRepository bars,
            IBarService barService,
            IHubContext<BarHub> barHub,
            IBarUserEntryRepository barUserEntries,
            IMapper mapper)
        {
            _barUserEntries = barUserEntries;
            _bars = bars;
            _barService = barService;
            _barHub = barHub;
            _mapper = mapper;
        }
        [HttpGet("all")]
        public async Task<ActionResult<List<BarDto>>> GetAllBars()
        {
            var bars = await _bars.GetAllAsync();
            if (bars == null || bars.Count == 0)
                return NotFound(StandardErrors.NotFound);

            var barDtos = _mapper.Map<List<BarDto>>(bars);
            return Ok(barDtos);
        }
        [HttpGet("default")]
        public async Task<ActionResult<BarDto>> GetDefaultBar()
        {
            var bar = await _barService.GetDefaultBar();
            if (bar == null)
                return NotFound(StandardErrors.NonexistentBar);

            var barDto = _mapper.Map<BarDto>(bar);
            return Ok(barDto);
        }

        [HttpGet("{barId}/isJoined")]
        public async Task<IActionResult> IsJoined(Guid barId)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
                return Unauthorized(StandardErrors.Unauthorized);

            if (!Guid.TryParse(userIdString, out Guid userId))
                return BadRequest(new { Code = "INVALID_USER_ID", Message = "User ID in session is invalid." });

            var result = await _barUserEntries.FindEntryAsync(barId, userId);

            return Ok(result.IsSuccess);
        }

        [HttpPost("{barId}/join")]
        public async Task<IActionResult> JoinBar(Guid barId)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
                return Unauthorized(StandardErrors.Unauthorized);

            if (!Guid.TryParse(userIdString, out Guid userId))
                return BadRequest(new { Code = "INVALID_USER_ID", Message = "User ID in session is invalid." });

            var entryResult = await _barService.EnterBar(barId, userId);
            var actionResult = this.ToActionResult(entryResult, "User joined bar successfully.");

            if (entryResult.IsSuccess)
            {
                // Notify all users in this bar that a new user joined
                await _barHub.Clients.Group(barId.ToString())
                    .SendAsync("BarUsersUpdated", new { userId });
            }

            return actionResult;
        }

        [HttpPost("{barId}/leave")]
        public async Task<IActionResult> LeaveBar(Guid barId)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
                return Unauthorized(StandardErrors.Unauthorized);

            if (!Guid.TryParse(userIdString, out Guid userId))
                return BadRequest(new { Code = "INVALID_USER_ID", Message = "User ID in session is invalid." });

            var entryResult = await _barService.LeaveBar(barId, userId);
            var actionResult = this.ToActionResult(entryResult, "Successfully left the bar.");

            if (entryResult.IsSuccess)
            {
                // Notify all users in this bar that a user left
                await _barHub.Clients.Group(barId.ToString())
                    .SendAsync("BarUsersUpdated", new { userId });
            }

            return actionResult;
        }
        [HttpGet("{barId}/users")]
        public async Task<ActionResult<List<object>>> GetConnectedUsers(Guid barId)
        {
            var users = await _barUserEntries.GetUsersInBarAsync(barId); // returns List<User>

            // Map manually
            var usersDto = users.Select(u => new
            {
                Id = u.Id,
                Username = u.Username
            }).ToList();

            return Ok(usersDto);
        }

    }
}
