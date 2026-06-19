using Application.DTOs.Notifications;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<NotificationDto>>> GetByUser([FromQuery] int userId)
        {
            var notifications = await _notificationService.GetByUserIdAsync(userId);
            return Ok(notifications);
        }

        [HttpGet("unread")]
        public async Task<ActionResult<IEnumerable<NotificationDto>>> GetUnread([FromQuery] int userId)
        {
            var notifications = await _notificationService.GetUnreadByUserIdAsync(userId);
            return Ok(notifications);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<NotificationDto>> GetById(int id)
        {
            var notification = await _notificationService.GetByIdAsync(id);
            if (notification is null) return NotFound();
            return Ok(notification);
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
            var success = await _notificationService.MarkAsReadAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _notificationService.DeleteAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }
    }
}
