using Domain.Entities;

namespace Application.Interfaces.Repositories
{
    public interface IFriendStreakInvitationRepository
    {
        Task<FriendStreakInvitation?> GetByIdAsync(int id);
        Task<FriendStreakInvitation?> GetPendingBetweenAsync(int userAId, int userBId);
        Task<IEnumerable<FriendStreakInvitation>> GetReceivedAsync(int userId);
        Task<IEnumerable<FriendStreakInvitation>> GetSentAsync(int userId);
        Task<FriendStreakInvitation> CreateAsync(FriendStreakInvitation invitation);
        Task<FriendStreakInvitation> UpdateAsync(FriendStreakInvitation invitation);
    }
}
