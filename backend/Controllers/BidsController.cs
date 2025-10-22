using backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using backend.Repositories.Interfaces;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CreditController : ControllerBase
    {
        private readonly IPlaylistService _playlistService;
        private readonly ICreditService _creditService;
        private readonly IUserRepository _users;

        public CreditController(IPlaylistService playlistService, ICreditService creditService, IUserRepository users)
        {
            _playlistService = playlistService;
            _creditService = creditService;
            _users = users;
        }

        // [HttpPost("place")]
        // public async Task<IActionResult> PlaceBid([FromQuery] Guid userId, [FromQuery] Guid songId, [FromQuery] int amount)
        // {
        //     var result = await _playlistService.BidOnSongAsync(userId, songId, amount);
        //     if (!result.IsSuccess) return BadRequest(result.Error?.Message);
        //     return Ok(result.Value);
        // }
    }
}