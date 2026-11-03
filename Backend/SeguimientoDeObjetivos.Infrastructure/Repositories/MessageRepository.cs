using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class MessageRepository : IMessageRepository
    {
        private readonly ApplicationDbContext _context;

        public MessageRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Message>> GetConversationAsync(int userAId, int userBId)
            => await _context.Messages
                .Where(m => (m.SenderId == userAId && m.ReceiverId == userBId) ||
                            (m.SenderId == userBId && m.ReceiverId == userAId))
                .OrderBy(m => m.SentAt)
                .ToListAsync();

        public async Task<IEnumerable<int>> GetUnreadSenderIdsAsync(int receiverId)
            => await _context.Messages
                .Where(m => m.ReceiverId == receiverId && m.ReadAt == null)
                .Select(m => m.SenderId)
                .Distinct()
                .ToListAsync();

        public async Task<IEnumerable<Message>> GetUnreadFromAsync(int receiverId, int senderId)
            => await _context.Messages
                .Where(m => m.ReceiverId == receiverId && m.SenderId == senderId && m.ReadAt == null)
                .ToListAsync();

        public Task<Message> CreateAsync(Message message)
        {
            _context.Messages.Add(message);
            return Task.FromResult(message);
        }

        public async Task DeleteAllForUserAsync(int userId)
        {
            var messages = await _context.Messages
                .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                .ToListAsync();
            _context.Messages.RemoveRange(messages);
        }
    }
}
