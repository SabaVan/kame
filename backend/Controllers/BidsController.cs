using backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BidsController : ControllerBase
    {
        private readonly IPlaylistService _playlistService;

        public BidsController(IPlaylistService playlistService)
        {
            _playlistService = playlistService;
        }

        [HttpPost("place")]
        public async Task<IActionResult> PlaceBid([FromQuery] Guid userId, [FromQuery] Guid songId, [FromQuery] int amount)
        {
            var result = await _playlistService.BidOnSongAsync(userId, songId, amount);
            if (!result.IsSuccess) return BadRequest(result.Error?.Message);
            return Ok(result.Value);
        }
    }
}