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

        // Las alertas son privadas: dicen a quien sigues, que objetivos cumpliste y
        // cuando. Este controller repetia el agujero que tuvo el diario: el userId
        // llegaba por la URL y no se contrastaba con nadie, asi que cambiando un
        // numero se leian, marcaban y borraban las alertas de cualquier usuario.
        [HttpGet]
        public async Task<ActionResult<IEnumerable<NotificationDto>>> GetByUser([FromQuery] int userId)
        {
            if (userId != RequesterId) return Forbid();

            return Ok(await _notificationService.GetByUserIdAsync(userId));
        }

        [HttpGet("unread")]
        public async Task<ActionResult<IEnumerable<NotificationDto>>> GetUnread([FromQuery] int userId)
        {
            if (userId != RequesterId) return Forbid();

            return Ok(await _notificationService.GetUnreadByUserIdAsync(userId));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<NotificationDto>> GetById(int id)
        {
            var notification = await _notificationService.GetByIdAsync(id);
            if (notification.UserId != RequesterId) return Forbid();

            return Ok(notification);
        }

        // Se quito POST: tomaba el UserId destino del cuerpo sin comprobar nada, asi
        // que cualquier usuario logueado podia fabricarle una alerta a otro con el
        // titulo y el texto que quisiera. El frontend nunca lo llamo; las alertas
        // reales las crean por dentro BadgeAwardService, FollowService y
        // ObjectiveService, que es la unica via legitima. Mismo criterio que el
        // POST assign que se quito de BadgesController.

        [HttpPatch("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notification = await _notificationService.GetByIdAsync(id);
            if (notification.UserId != RequesterId) return Forbid();

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
            var notification = await _notificationService.GetByIdAsync(id);
            if (notification.UserId != RequesterId) return Forbid();

            await _notificationService.DeleteAsync(id);
            return NoContent();
        }
    }
}
