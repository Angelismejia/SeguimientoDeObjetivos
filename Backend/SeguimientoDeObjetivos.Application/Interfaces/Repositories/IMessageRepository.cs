using Domain.Entities;

namespace Application.Interfaces.Repositories
{
    public interface IMessageRepository
    {
        Task<IEnumerable<Message>> GetConversationAsync(int userAId, int userBId);
        Task<Message> CreateAsync(Message message);
    }
}
