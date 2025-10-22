using backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
  [ApiController]
  [Route("api/[controller]")]
  public class SongsController : ControllerBase
  {
    private readonly ISongService _songService;

    public SongsController(ISongService songService)
    {
      _songService = songService;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string query, [FromQuery] int limit = 10)
    {
      var songs = await _songService.SearchSongsAsync(query, limit);
      return Ok(songs);
    }
  }
}