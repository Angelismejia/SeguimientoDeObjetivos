using System.Security.Claims;
using Application.DTOs.Messages;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Api.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IMessageService _messageService;

        public ChatHub(IMessageService messageService)
        {
            _messageService = messageService;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
            }
            await base.OnConnectedAsync();
        }

        public async Task SendMessage(CreateMessageDto dto)
        {
            var senderId = int.Parse(Context.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var message = await _messageService.SendAsync(senderId, dto);

            await Clients.Group($"user-{dto.ReceiverId}").SendAsync("ReceiveMessage", message);
            await Clients.Group($"user-{senderId}").SendAsync("ReceiveMessage", message);
        }
    }
}
