using Application.DTOs.FriendStreaks;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class FriendStreaksController : ControllerBase
    {
        private readonly IFriendStreakService _friendStreakService;

        public FriendStreaksController(IFriendStreakService friendStreakService)
        {
            _friendStreakService = friendStreakService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<FriendStreakDto>>> GetForUser([FromQuery] int userId)
        {
            return Ok(await _friendStreakService.GetForUserAsync(userId));
        }

        [HttpGet("invitations/received")]
        public async Task<ActionResult<IEnumerable<FriendStreakInvitationDto>>> GetReceivedInvitations([FromQuery] int userId)
        {
            return Ok(await _friendStreakService.GetReceivedInvitationsAsync(userId));
        }

        [HttpGet("invitations/sent")]
        public async Task<ActionResult<IEnumerable<FriendStreakInvitationDto>>> GetSentInvitations([FromQuery] int userId)
        {
            return Ok(await _friendStreakService.GetSentInvitationsAsync(userId));
        }

        [HttpPost("invitations")]
        public async Task<ActionResult<FriendStreakInvitationDto>> Invite([FromQuery] int fromUserId, CreateFriendStreakInvitationDto dto)
        {
            return Ok(await _friendStreakService.InviteAsync(fromUserId, dto));
        }

        [HttpPost("invitations/{id}/accept")]
        public async Task<ActionResult<FriendStreakDto>> Accept(int id, [FromQuery] int userId)
        {
            return Ok(await _friendStreakService.AcceptAsync(id, userId));
        }

        [HttpPost("invitations/{id}/reject")]
        public async Task<IActionResult> Reject(int id, [FromQuery] int userId)
        {
            await _friendStreakService.RejectAsync(id, userId);
            return NoContent();
        }
    }
}
