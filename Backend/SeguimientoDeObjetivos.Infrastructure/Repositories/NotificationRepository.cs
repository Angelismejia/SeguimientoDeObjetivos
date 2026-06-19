using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly ApplicationDbContext _context;

        public NotificationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Notification?> GetByIdAsync(int id)
            => await _context.Notifications.FindAsync(id);

        public async Task<IEnumerable<Notification>> GetByUserIdAsync(int userId)
            => await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

        public async Task<IEnumerable<Notification>> GetUnreadByUserIdAsync(int userId)
            => await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

        public Task<Notification> CreateAsync(Notification notification)
        {
            _context.Notifications.Add(notification);
            return Task.FromResult(notification);
        }

        public async Task<bool> MarkAsReadAsync(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification is null) return false;
            notification.IsRead = true;
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification is null) return false;
            _context.Notifications.Remove(notification);
            return true;
        }
    }
}
