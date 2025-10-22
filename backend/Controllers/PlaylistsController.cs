using AutoMapper;
using backend.Utils.Errors;
using backend.Hubs;
using backend.Models;
using backend.Repositories.Interfaces;
using backend.Services.Interfaces;
using backend.Shared.DTOs;
using backend.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlaylistsController : ControllerBase
    {
        private readonly IPlaylistService _playlistService;
        private readonly IBarPlaylistEntryRepository _barPlaylistEntries;
        private readonly IBarUserEntryRepository _barUserEntries;
        private readonly IHubContext<BarHub> _barHub;
        private readonly IMapper _mapper;

        public PlaylistsController(
            IPlaylistService playlistService,
            IBarPlaylistEntryRepository barPlaylistEntries,
            IBarUserEntryRepository barUserEntries,
            IHubContext<BarHub> barHub,
            IMapper mapper)
        {
            _playlistService = playlistService;
            _barPlaylistEntries = barPlaylistEntries;
            _barUserEntries = barUserEntries;
            _barHub = barHub;
            _mapper = mapper;
        }

        // ✅ Get playlist
        [HttpGet("{playlistId}")]
        public async Task<ActionResult<PlaylistDto>> GetPlaylist(Guid playlistId)
        {
            var result = await _playlistService.GetByIdAsync(playlistId);
            if (!result.IsSuccess)
                return NotFound(result.Error?.Message);

            var dto = _mapper.Map<PlaylistDto>(result.Value);
            return Ok(dto);
        }

        // ✅ Add song
        [HttpPost("{playlistId}/add-song")]
        public async Task<IActionResult> AddSong(Guid playlistId, [FromBody] Song song)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
                return Unauthorized(StandardErrors.Unauthorized);

            if (!Guid.TryParse(userIdString, out Guid userId))
                return BadRequest(new { Code = "INVALID_USER_ID", Message = "User ID in session is invalid." });

            var result = await _playlistService.AddSongAsync(userId, song);
            var actionResult = this.ToActionResult(result, "Song added to playlist successfully.");

            if (result.IsSuccess)
            {
                // 🔍 Find which bar(s) this playlist is linked to
                var bars = await _barPlaylistEntries.GetBarsForPlaylistAsync(playlistId);

                foreach (var bar in bars)
                {
                    await _barHub.Clients.Group(bar.Id.ToString())
                        .SendAsync("PlaylistUpdated", new
                        {
                            playlistId,
                            userId,
                            songId = song.Id,
                            songTitle = song.Title,
                            action = "song_added"
                        });
                }
            }

            return actionResult;
        }

        // ✅ Reorder playlist
        [HttpPost("{playlistId}/reorder")]
        public async Task<IActionResult> Reorder(Guid playlistId)
        {
            var result = await _playlistService.ReorderAndSavePlaylistAsync(playlistId);
            var actionResult = this.ToActionResult(result, "Playlist reordered successfully.");

            if (result.IsSuccess)
            {
                // 🔍 Notify all bars linked to this playlist
                var bars = await _barPlaylistEntries.GetBarsForPlaylistAsync(playlistId);

                foreach (var bar in bars)
                {
                    await _barHub.Clients.Group(bar.Id.ToString())
                        .SendAsync("PlaylistUpdated", new
                        {
                            playlistId,
                            userId = HttpContext.Session.GetString("UserId"),
                            action = "reordered"
                        });
                }
            }

            return actionResult;
        }

        // ✅ Get next song
        [HttpGet("{playlistId}/next-song")]
        public async Task<IActionResult> GetNextSong(Guid playlistId)
        {
            var result = await _playlistService.GetNextSongAsync(playlistId);
            if (!result.IsSuccess)
                return NotFound(result.Error?.Message);

            return Ok(result.Value);
        }

        // ✅ Get users connected to any bar linked to this playlist
        [HttpGet("{playlistId}/users")]
        public async Task<ActionResult<List<object>>> GetConnectedUsers(Guid playlistId)
        {
            var bars = await _barPlaylistEntries.GetBarsForPlaylistAsync(playlistId);
            var users = new List<User>();

            foreach (var bar in bars)
            {
                var barUsers = await _barUserEntries.GetUsersInBarAsync(bar.Id);
                users.AddRange(barUsers);
            }

            // Remove duplicates (same user in multiple bars)
            users = users.DistinctBy(u => u.Id).ToList();

            var usersDto = users.Select(u => new
            {
                Id = u.Id,
                Username = u.Username
            }).ToList();

            return Ok(usersDto);
        }
    }
}
