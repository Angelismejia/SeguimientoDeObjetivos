using Application.DTOs.FriendStreaks;

namespace Application.Interfaces.Services
{
    public interface IFriendStreakService
    {
        Task<IEnumerable<FriendStreakInvitationDto>> GetReceivedInvitationsAsync(int userId);
        Task<IEnumerable<FriendStreakInvitationDto>> GetSentInvitationsAsync(int userId);
        Task<FriendStreakInvitationDto> InviteAsync(int fromUserId, CreateFriendStreakInvitationDto dto);
        Task<FriendStreakDto> AcceptAsync(int invitationId, int currentUserId);
        Task RejectAsync(int invitationId, int currentUserId);
        Task<IEnumerable<FriendStreakDto>> GetForUserAsync(int userId);
    }
}
