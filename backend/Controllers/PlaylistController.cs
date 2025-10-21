using AutoMapper;
using backend.Hubs;
using backend.Models;
using backend.Repositories.Interfaces;
using backend.Services.Interfaces;
using backend.Shared.DTOs;
using backend.Utils;
using backend.Utils.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlaylistController : ControllerBase
    {
        private readonly IPlaylistService _playlistService;
        private readonly IBarRepository _bars;
        private readonly IHubContext<BarHub> _barHub;
        private readonly IMapper _mapper;

        // TEMPORARY — replace with user claims when auth is integrated
        private readonly Guid userId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        public PlaylistController(
            IPlaylistService playlistService,
            IBarRepository bars,
            IHubContext<BarHub> barHub,
            IMapper mapper)
        {
            _playlistService = playlistService;
            _bars = bars;
            _barHub = barHub;
            _mapper = mapper;
        }

 /*        // ✅ Get playlist for a bar
        [HttpGet("{barId}")]
        public async Task<ActionResult<PlaylistDto>> GetPlaylist(Guid barId)
        {
            var playlistResult = await _playlistService.GetPlaylistForBarAsync(barId);
            if (!playlistResult.IsSuccess)
                return NotFound(StandardErrors.NonexistentPlaylist);

            return Ok(playlistResult.Value);
        }

        // ✅ Add a song to the bar playlist
        [HttpPost("{barId}/add")]
        public async Task<IActionResult> AddSong(Guid barId, [FromBody] SongCreateDto songDto)
        {
            var addResult = await _playlistService.AddSongAsync(barId, songDto, userId);
            var actionResult = this.ToActionResult(addResult, "Song added successfully.");

            if (addResult.IsSuccess)
            {
                await _barHub.Clients.Group(barId.ToString())
                    .SendAsync("PlaylistUpdated", new { barId });
            }

            return actionResult;
        }

        // ✅ Remove song
        [HttpDelete("{barId}/remove/{songId}")]
        public async Task<IActionResult> RemoveSong(Guid barId, Guid songId)
        {
            var removeResult = await _playlistService.RemoveSongAsync(barId, songId, userId);
            var actionResult = this.ToActionResult(removeResult, "Song removed successfully.");

            if (removeResult.IsSuccess)
            {
                await _barHub.Clients.Group(barId.ToString())
                    .SendAsync("PlaylistUpdated", new { barId });
            }

            return actionResult;
        }

        // ✅ Upvote or downvote a song
        [HttpPost("{barId}/vote")]
        public async Task<IActionResult> VoteSong(Guid barId, [FromBody] SongVoteDto voteDto)
        {
            var voteResult = await _playlistService.VoteSongAsync(barId, voteDto, userId);
            var actionResult = this.ToActionResult(voteResult, "Vote registered successfully.");

            if (voteResult.IsSuccess)
            {
                await _barHub.Clients.Group(barId.ToString())
                    .SendAsync("PlaylistUpdated", new { barId });
            }

            return actionResult;
        } */
    }
}
