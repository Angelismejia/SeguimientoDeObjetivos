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

        [HttpPost]
        public async Task<ActionResult<FollowDto>> Create([FromQuery] int followerId, CreateFollowDto dto)
        {
            var created = await _followService.CreateAsync(followerId, dto);
            return Ok(created);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete([FromQuery] int followerId, [FromQuery] int followingId)
        {
            await _followService.DeleteAsync(followerId, followingId);
            return NoContent();
        }
    }
}
