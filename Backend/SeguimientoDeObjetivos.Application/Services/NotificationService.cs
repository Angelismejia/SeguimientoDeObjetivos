using Application.DTOs.Notifications;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;

namespace Application.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IUnitOfWork _unitOfWork;

        public NotificationService(INotificationRepository notificationRepository, IUnitOfWork unitOfWork)
        {
            _notificationRepository = notificationRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<NotificationDto>> GetByUserIdAsync(int userId)
        {
            var notifications = await _notificationRepository.GetByUserIdAsync(userId);
            return notifications.Select(ToDto);
        }

        public async Task<IEnumerable<NotificationDto>> GetUnreadByUserIdAsync(int userId)
        {
            var notifications = await _notificationRepository.GetUnreadByUserIdAsync(userId);
            return notifications.Select(ToDto);
        }

        public async Task<NotificationDto?> GetByIdAsync(int id)
        {
            var notification = await _notificationRepository.GetByIdAsync(id);
            return notification is null ? null : ToDto(notification);
        }

        public async Task<NotificationDto> CreateAsync(CreateNotificationDto dto)
        {
            var notification = new Notification
            {
                UserId = dto.UserId,
                TaskId = dto.TaskId,
                Title = dto.Title,
                Message = dto.Message,
                Type = dto.Type,
                SendEmail = dto.SendEmail
            };

            var created = await _notificationRepository.CreateAsync(notification);
            await _unitOfWork.SaveChangesAsync();
            return ToDto(created);
        }

        public async Task<bool> MarkAsReadAsync(int id)
        {
            var marked = await _notificationRepository.MarkAsReadAsync(id);
            if (!marked) return false;
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var deleted = await _notificationRepository.DeleteAsync(id);
            if (!deleted) return false;
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

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
