using backend.Models;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
  [ApiController]
  [Route("api/[controller]")]
  public class PlaylistsController : ControllerBase
  {
    private readonly IPlaylistService _playlistService;

    public PlaylistsController(IPlaylistService playlistService)
    {
      _playlistService = playlistService;
    }

    [HttpGet("{playlistId}")]
    public async Task<IActionResult> GetPlaylist(Guid playlistId)
    {
      var result = await _playlistService.GetByIdAsync(playlistId);
      if (!result.IsSuccess) return NotFound(result.Error?.Message);
      return Ok(result.Value);
    }

    [HttpPost("{playlistId}/add-song")]
    public async Task<IActionResult> AddSong(Guid playlistId, [FromBody] Song song, [FromQuery] Guid userId)
    {
      var result = await _playlistService.AddSongAsync(userId, song);
      if (!result.IsSuccess) return BadRequest(result.Error?.Message);
      return Ok(result.Value);
    }

    [HttpPost("{playlistId}/reorder")]
    public async Task<IActionResult> Reorder(Guid playlistId)
    {
      var result = await _playlistService.ReorderAndSavePlaylistAsync(playlistId);
      if (!result.IsSuccess) return NotFound(result.Error?.Message);
      return Ok(result.Value);
    }

    [HttpGet("{playlistId}/next-song")]
    public async Task<IActionResult> GetNextSong(Guid playlistId)
    {
      var result = await _playlistService.GetNextSongAsync(playlistId);
      if (!result.IsSuccess) return NotFound(result.Error?.Message);
      return Ok(result.Value);
    }
  }
}