using Application.DTOs.Badges;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
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
            var badges = await _badgeService.GetAllAsync();
            return Ok(badges);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BadgeDto>> GetById(int id)
        {
            var badge = await _badgeService.GetByIdAsync(id);
            if (badge is null) return NotFound();
            return Ok(badge);
        }

        [HttpGet("by-user/{userId}")]
        public async Task<ActionResult<IEnumerable<BadgeDto>>> GetByUser(int userId)
        {
            var badges = await _badgeService.GetByUserIdAsync(userId);
            return Ok(badges);
        }

        [HttpPost("assign")]
        public async Task<IActionResult> Assign([FromQuery] int userId, [FromQuery] int badgeId)
        {
            var success = await _badgeService.AssignToUserAsync(userId, badgeId);
            if (!success) return Conflict("El usuario ya tiene este badge.");
            return NoContent();
        }
    }
}
