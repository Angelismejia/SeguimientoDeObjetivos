using Application.DTOs.Messages;

namespace Application.Interfaces.Services
{
    public interface IMessageService
    {
        Task<IEnumerable<MessageDto>> GetConversationAsync(int userAId, int userBId);
        Task<MessageDto> SendAsync(int senderId, CreateMessageDto dto);
        Task<IEnumerable<int>> GetUnreadSenderIdsAsync(int receiverId);
        Task MarkAsReadAsync(int receiverId, int senderId);
    }
}
