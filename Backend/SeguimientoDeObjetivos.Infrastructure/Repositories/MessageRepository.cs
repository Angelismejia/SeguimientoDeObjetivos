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

        public Task<Message> CreateAsync(Message message)
        {
            _context.Messages.Add(message);
            return Task.FromResult(message);
        }
    }
}
