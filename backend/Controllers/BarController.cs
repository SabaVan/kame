using Microsoft.AspNetCore.Mvc;
using backend.Models;
using backend.Exceptions.Bar;
using backend.Services;
using backend.Enums;
namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BarController : ControllerBase
    {
        private readonly SimpleBarService _barService;

        public BarController(SimpleBarService barService)
        {
            _barService = barService;
        }

        // GET: /api/bar/{id}/state
        [HttpGet("{id:guid}/state")]
        public async Task<ActionResult<string>> GetState(Guid id)
        {
            var bar = await _barService.GetBarByIdAsync(id);
            if (bar == null) return NotFound("Bar not found");

            return Ok(bar.State.ToString());
        }
    }
}
