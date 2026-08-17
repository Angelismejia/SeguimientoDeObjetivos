using System.Security.Claims;
using Application.DTOs.Notifications;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        private int RequesterId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpGet]
        public async Task<ActionResult<IEnumerable<NotificationDto>>> GetByUser([FromQuery] int userId)
        {
            return Ok(await _notificationService.GetByUserIdAsync(userId));
        }

        [HttpGet("unread")]
        public async Task<ActionResult<IEnumerable<NotificationDto>>> GetUnread([FromQuery] int userId)
        {
            return Ok(await _notificationService.GetUnreadByUserIdAsync(userId));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<NotificationDto>> GetById(int id)
        {
            return Ok(await _notificationService.GetByIdAsync(id));
        }

        [HttpPost]
        public async Task<ActionResult<NotificationDto>> Create(CreateNotificationDto dto)
        {
            var created = await _notificationService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPatch("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            await _notificationService.MarkAsReadAsync(id);
            return NoContent();
        }

        // Marca todas las del usuario. El userId no se toma de la query para que
        // nadie pueda marcar las notificaciones de otro: sale del token.
        [HttpPatch("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            await _notificationService.MarkAllAsReadAsync(RequesterId);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _notificationService.DeleteAsync(id);
            return NoContent();
        }
    }
}
