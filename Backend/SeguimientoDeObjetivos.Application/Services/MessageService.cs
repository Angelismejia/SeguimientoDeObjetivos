using Application.Common;
using Application.DTOs.Messages;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Exceptions;

namespace Application.Services
{
    public class MessageService : IMessageService
    {
        // Mismo tope que el [MaxLength] de CreateMessageDto. Se repite aca porque
        // el chat entra por SignalR, y SignalR NO corre las validaciones de los
        // DTOs: sin esto, el hub guardaba mensajes vacios o de megabytes.
        public const int LargoMaximo = 2000;

        private readonly IMessageRepository _messageRepository;
        private readonly IUserRepository _userRepository;
        private readonly IFollowRepository _followRepository;
        private readonly IUnitOfWork _unitOfWork;

        public MessageService(
            IMessageRepository messageRepository,
            IUserRepository userRepository,
            IFollowRepository followRepository,
            IUnitOfWork unitOfWork)
        {
            _messageRepository = messageRepository;
            _userRepository = userRepository;
            _followRepository = followRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<MessageDto>> GetConversationAsync(int userAId, int userBId)
        {
            var messages = await _messageRepository.GetConversationAsync(userAId, userBId);
            return messages.Select(ToDto);
        }

        public async Task<IEnumerable<MessageDto>> GetConversationPageAsync(int userAId, int userBId, int limit, int? beforeId)
        {
            var messages = await _messageRepository.GetConversationPageAsync(userAId, userBId, Paginacion.Limite(limit), beforeId);
            return messages.Select(ToDto);
        }

        public async Task<MessageDto> SendAsync(int senderId, CreateMessageDto dto)
        {
            var content = dto.Content?.Trim() ?? string.Empty;
            if (content.Length == 0)
                throw new BusinessRuleException("El mensaje está vacío.");
            if (content.Length > LargoMaximo)
                throw new BusinessRuleException($"El mensaje no puede pasar de {LargoMaximo} caracteres.");
            if (dto.ReceiverId == senderId)
                throw new BusinessRuleException("No puedes enviarte mensajes a ti mismo.");

            // Antes un ReceiverId inexistente llegaba hasta la base y reventaba con
            // un error de clave foranea.
            var receiver = await _userRepository.GetByIdAsync(dto.ReceiverId);
            if (receiver is null || !receiver.IsActive)
                throw new BusinessRuleException("Esa persona no está disponible.");

            // Los contactos del chat son seguidos + seguidores, asi que la regla es
            // la misma: basta con que uno de los dos siga al otro. Sin esto, cualquiera
            // podia escribirle a cualquier id, aunque no se conocieran.
            var seConocen = await _followRepository.GetAsync(senderId, dto.ReceiverId) is not null
                || await _followRepository.GetAsync(dto.ReceiverId, senderId) is not null;
            if (!seConocen)
                throw new BusinessRuleException("Solo puedes escribirle a quien sigues o te sigue.");

            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = dto.ReceiverId,
                Content = content
            };

            var created = await _messageRepository.CreateAsync(message);
            await _unitOfWork.SaveChangesAsync();

            return ToDto(created);
        }

        public Task<IEnumerable<int>> GetUnreadSenderIdsAsync(int receiverId)
            => _messageRepository.GetUnreadSenderIdsAsync(receiverId);

        // Solo se marcan los mensajes que RECIBIO quien lee: los que el mismo
        // mando en esa conversacion los marca el otro al abrirla.
        public async Task MarkAsReadAsync(int receiverId, int senderId)
        {
            var unread = (await _messageRepository.GetUnreadFromAsync(receiverId, senderId)).ToList();
            if (unread.Count == 0) return;

            var now = DateTime.UtcNow;
            foreach (var message in unread)
                message.ReadAt = now;

            await _unitOfWork.SaveChangesAsync();
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
