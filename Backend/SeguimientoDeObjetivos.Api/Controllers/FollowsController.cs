using System.Security.Claims;
using Application.DTOs.Follows;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class FollowsController : ControllerBase
    {
        private readonly IFollowService _followService;

        public FollowsController(IFollowService followService)
        {
            _followService = followService;
        }

        private int RequesterId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpGet("followers")]
        public async Task<ActionResult<IEnumerable<UserSummaryDto>>> GetFollowers([FromQuery] int userId)
        {
            return Ok(await _followService.GetFollowersAsync(userId));
        }

        [HttpGet("following")]
        public async Task<ActionResult<IEnumerable<UserSummaryDto>>> GetFollowing([FromQuery] int userId)
        {
            return Ok(await _followService.GetFollowingAsync(userId));
        }

        // Quien sigue sale del token y ya no del query. Recibiendolo por la URL,
        // cualquiera podia hacer que otra persona siguiera a quien quisiera, o
        // deshacer amistades ajenas.
        //
        // Las listas de seguidores de arriba si son consultables: el perfil de un
        // amigo las muestra, y son informacion social, no privada.
        [HttpPost]
        public async Task<ActionResult<FollowDto>> Create(CreateFollowDto dto)
        {
            var created = await _followService.CreateAsync(RequesterId, dto);
            return Ok(created);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete([FromQuery] int followingId)
        {
            await _followService.DeleteAsync(RequesterId, followingId);
            return NoContent();
        }
    }
}
