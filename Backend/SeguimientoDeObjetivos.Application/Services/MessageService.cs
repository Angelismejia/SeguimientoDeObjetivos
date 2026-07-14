using Application.DTOs.Messages;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;

namespace Application.Services
{
    public class MessageService : IMessageService
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IUnitOfWork _unitOfWork;

        public MessageService(IMessageRepository messageRepository, IUnitOfWork unitOfWork)
        {
            _messageRepository = messageRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<MessageDto>> GetConversationAsync(int userAId, int userBId)
        {
            var messages = await _messageRepository.GetConversationAsync(userAId, userBId);
            return messages.Select(ToDto);
        }

        public async Task<MessageDto> SendAsync(int senderId, CreateMessageDto dto)
        {
            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = dto.ReceiverId,
                Content = dto.Content
            };

            var created = await _messageRepository.CreateAsync(message);
            await _unitOfWork.SaveChangesAsync();

            return ToDto(created);
        }

        private static MessageDto ToDto(Message m) => new()
        {
            Id = m.Id,
            SenderId = m.SenderId,
            ReceiverId = m.ReceiverId,
            Content = m.Content,
            SentAt = m.SentAt,
            ReadAt = m.ReadAt
        };
    }
}
