using Application.DTOs.Notifications;      
using Application.Interfaces.Repositories; 
using Application.Interfaces.Services;     
using Domain.Entities;                  

namespace Application.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;

        public NotificationService(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        // devuelve todas las notificaciones de un usuario, ordenadas por fecha (el repositorio ya las ordena)
        public async Task<IEnumerable<NotificationDto>> GetByUserIdAsync(int userId)
        {
            var notifications = await _notificationRepository.GetByUserIdAsync(userId);
            return notifications.Select(ToDto);
        }

        // devuelve solo las notificaciones no leídas (útil para el contador de la UI)
        public async Task<IEnumerable<NotificationDto>> GetUnreadByUserIdAsync(int userId)
        {
            var notifications = await _notificationRepository.GetUnreadByUserIdAsync(userId);
            return notifications.Select(ToDto);
        }

        // devuelve una notificación por id
        public async Task<NotificationDto?> GetByIdAsync(int id)
        {
            var notification = await _notificationRepository.GetByIdAsync(id);
            return notification is null ? null : ToDto(notification);
        }

        // crea una notificación nueva
        // las notificaciones las genera el sistema, no el usuario, por eso no recibe userId por separado
        // el userId viene dentro del CreateNotificationDto
        public async Task<NotificationDto> CreateAsync(CreateNotificationDto dto)
        {
            var notification = new Notification
            {
                UserId = dto.UserId,
                TaskId = dto.TaskId,     // nullable: puede no estar vinculada a una tarea
                Title = dto.Title,
                Message = dto.Message,
                Type = dto.Type,         // "Reminder", "Achievement", "System", etc.
                SendEmail = dto.SendEmail // si también se debe enviar por correo
                // IsRead arranca en false por defecto (definido en la entidad)
            };

            var created = await _notificationRepository.CreateAsync(notification);
            return ToDto(created);
        }

        // marca una notificación como leída
        // devuelve false si la notificación no existe
        public async Task<bool> MarkAsReadAsync(int id)
        {
            return await _notificationRepository.MarkAsReadAsync(id); // el repositorio hace el cambio de IsRead = true
        }

        // elimina una notificación
        public async Task<bool> DeleteAsync(int id)
        {
            return await _notificationRepository.DeleteAsync(id);
        }

        // convierte la entidad Notification al DTO de respuesta
        private static NotificationDto ToDto(Notification n) => new()
        {
            Id = n.Id,
            UserId = n.UserId,
            TaskId = n.TaskId,
            Title = n.Title,
            Message = n.Message,
            Type = n.Type,
            IsRead = n.IsRead,
            SendEmail = n.SendEmail,
            SentAt = n.SentAt,
            CreatedAt = n.CreatedAt
        };
    }
}
