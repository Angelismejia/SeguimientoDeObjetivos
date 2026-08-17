using Application.DTOs.Notifications;

namespace Application.Interfaces.Services
{
    public interface INotificationService
    {
        Task<IEnumerable<NotificationDto>> GetByUserIdAsync(int userId);
        Task<IEnumerable<NotificationDto>> GetUnreadByUserIdAsync(int userId);
        Task<NotificationDto> GetByIdAsync(int id);
        Task<NotificationDto> CreateAsync(CreateNotificationDto dto);
        Task MarkAsReadAsync(int id);
        Task<int> MarkAllAsReadAsync(int userId);
        Task DeleteAsync(int id);
    }
}
