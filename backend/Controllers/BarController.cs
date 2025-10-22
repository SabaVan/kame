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

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BarController : ControllerBase
    {
        Guid userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        // var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        private readonly IBarRepository _bars;
        private readonly IBarUserEntryRepository _barUserEntries;
        private readonly IBarService _barService;
        private readonly IUserRepository _users;
        private readonly IHubContext<BarHub> _barHub;
        private readonly IMapper _mapper;

        public BarController(
            IBarRepository bars,
            IBarService barService,
            IUserRepository users,
            IBarUserEntryRepository barUserEntries,
            IHubContext<BarHub> barHub,
            IMapper mapper)
        {
            _bars = bars;
            _barService = barService;
            _barUserEntries = barUserEntries;
            _users = users;
            _barHub = barHub;
            _mapper = mapper;
        }

        [HttpGet("all")]
        public async Task<ActionResult<List<BarDto>>> GetAllBars()
        {
            var bars = await _bars.GetAllAsync();
            if (bars == null || bars.Count == 0)
                return NotFound(StandardErrors.NotFound);

            await _barService.CheckSchedule(DateTime.UtcNow);
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
            var result = await _barUserEntries.FindEntryAsync(barId, userId);

            return Ok(result.IsSuccess);
        }

        //[Authorize]
        [HttpPost("{barId}/join")]
        public async Task<IActionResult> JoinBar(Guid barId)
        {
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

        //[Authorize]
        [HttpPost("{barId}/leave")]
        public async Task<IActionResult> LeaveBar(Guid barId)
        {
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
