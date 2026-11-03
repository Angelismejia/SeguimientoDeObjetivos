using Domain.Entities;

namespace Application.Interfaces.Repositories
{
    public interface IMessageRepository
    {
        Task<IEnumerable<Message>> GetConversationAsync(int userAId, int userBId);
        Task<IEnumerable<Message>> GetConversationPageAsync(int userAId, int userBId, int limit, int? beforeId);
        Task<Message> CreateAsync(Message message);
        Task<IEnumerable<int>> GetUnreadSenderIdsAsync(int receiverId);
        Task<IEnumerable<Message>> GetUnreadFromAsync(int receiverId, int senderId);
        Task DeleteAllForUserAsync(int userId);
    }
}
