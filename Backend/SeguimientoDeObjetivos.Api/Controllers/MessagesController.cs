using System.Security.Claims;
using Application.DTOs.Messages;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class MessagesController : ControllerBase
    {
        private readonly IMessageService _messageService;

        public MessagesController(IMessageService messageService)
        {
            _messageService = messageService;
        }

        [HttpGet("conversation")]
        // Sin limit devuelve la conversacion entera, como antes. Con limit, los
        // mensajes anteriores a beforeId (el mas viejo que ya tiene el front).
        public async Task<ActionResult<IEnumerable<MessageDto>>> GetConversation(
            [FromQuery] int userAId, [FromQuery] int userBId,
            [FromQuery] int? limit = null, [FromQuery] int? beforeId = null)
        {
            var requesterId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            if (requesterId != userAId && requesterId != userBId)
                return Forbid();

            if (limit is null)
                return Ok(await _messageService.GetConversationAsync(userAId, userBId));

            return Ok(await _messageService.GetConversationPageAsync(userAId, userBId, limit.Value, beforeId));
        }

        // Ninguno de los dos recibe userId: el lector sale del token, asi que no
        // hay forma de consultar ni marcar los mensajes de otro.
        [HttpGet("unread-senders")]
        public async Task<ActionResult<IEnumerable<int>>> GetUnreadSenders()
        {
            var requesterId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            return Ok(await _messageService.GetUnreadSenderIdsAsync(requesterId));
        }

        [HttpPut("read/{senderId}")]
        public async Task<IActionResult> MarkAsRead(int senderId)
        {
            var requesterId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            await _messageService.MarkAsReadAsync(requesterId, senderId);
            return NoContent();
        }
    }
}
