using Application.DTOs.Badges;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class BadgesController : ControllerBase
    {
        private readonly IBadgeService _badgeService;

        public BadgesController(IBadgeService badgeService)
        {
            _badgeService = badgeService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BadgeDto>>> GetAll()
        {
            return Ok(await _badgeService.GetAllAsync());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BadgeDto>> GetById(int id)
        {
            return Ok(await _badgeService.GetByIdAsync(id));
        }

        [HttpGet("by-user/{userId}")]
        public async Task<ActionResult<IEnumerable<BadgeDto>>> GetByUser(int userId)
        {
            return Ok(await _badgeService.GetByUserIdAsync(userId));
        }

        [HttpPost("assign")]
        public async Task<IActionResult> Assign([FromQuery] int userId, [FromQuery] int badgeId)
        {
            var assigned = await _badgeService.AssignToUserAsync(userId, badgeId);
            if (!assigned) return Conflict("El usuario ya tiene este badge.");
            return NoContent();
        }
    }
}
