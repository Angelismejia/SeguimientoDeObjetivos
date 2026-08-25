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

        // Se quito POST assign: recibia userId y badgeId por query sin comprobar
        // nada, asi que cualquier usuario logueado podia regalarse todas las
        // insignias o ponerselas a otro. El frontend nunca lo llamaba, y las
        // insignias se otorgan solas desde BadgeAwardService al completar tareas
        // y objetivos, que es la unica via legitima.
    }
}
