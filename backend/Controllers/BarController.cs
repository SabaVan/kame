using AutoMapper;
using backend.DTOs;
using backend.Models;
using backend.Repositories.Interfaces;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BarController : ControllerBase
    {
        private readonly IBarRepository _bars;
        private readonly IBarService _barService;
        private readonly IMapper _mapper;

        public BarController(IBarRepository bars, IBarService barService, IMapper mapper)
        {
            _bars = bars;
            _barService = barService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<List<BarDto>>> GetAllBars()
        {
            var bars = await _bars.GetAllAsync();
            if (bars == null || bars.Count == 0)
                return NotFound("No bars found");

            var barDtos = _mapper.Map<List<BarDto>>(bars); 
            return Ok(barDtos);
        }
    }
}
