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
        public async Task<ActionResult<IEnumerable<MessageDto>>> GetConversation([FromQuery] int userAId, [FromQuery] int userBId)
        {
            var requesterId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            if (requesterId != userAId && requesterId != userBId)
                return Forbid();


            return Ok(await _messageService.GetConversationAsync(userAId, userBId));
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
