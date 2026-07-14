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
            return Ok(await _messageService.GetConversationAsync(userAId, userBId));
        }
    }
}
