using System.Security.Claims;
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

        private int RequesterId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // Todos toman la identidad del token. Antes llegaba por query, asi que se
        // podian ver las invitaciones de cualquiera, invitar haciendose pasar por
        // otro, y aceptar o rechazar en su nombre.
        //
        // AcceptAsync y RejectAsync ya comprobaban que la invitacion fuera del
        // usuario, pero contra el mismo numero que mandaba quien llamaba: bastaba
        // con enviar el correcto para saltear la comprobacion.
        [HttpGet]
        public async Task<ActionResult<IEnumerable<FriendStreakDto>>> GetForUser()
        {
            return Ok(await _friendStreakService.GetForUserAsync(RequesterId));
        }

        [HttpGet("invitations/received")]
        public async Task<ActionResult<IEnumerable<FriendStreakInvitationDto>>> GetReceivedInvitations()
        {
            return Ok(await _friendStreakService.GetReceivedInvitationsAsync(RequesterId));
        }

        [HttpGet("invitations/sent")]
        public async Task<ActionResult<IEnumerable<FriendStreakInvitationDto>>> GetSentInvitations()
        {
            return Ok(await _friendStreakService.GetSentInvitationsAsync(RequesterId));
        }

        [HttpPost("invitations")]
        public async Task<ActionResult<FriendStreakInvitationDto>> Invite(CreateFriendStreakInvitationDto dto)
        {
            return Ok(await _friendStreakService.InviteAsync(RequesterId, dto));
        }

        [HttpPost("invitations/{id}/accept")]
        public async Task<ActionResult<FriendStreakDto>> Accept(int id)
        {
            return Ok(await _friendStreakService.AcceptAsync(id, RequesterId));
        }

        [HttpPost("invitations/{id}/reject")]
        public async Task<IActionResult> Reject(int id)
        {
            await _friendStreakService.RejectAsync(id, RequesterId);
            return NoContent();
        }
    }
}
