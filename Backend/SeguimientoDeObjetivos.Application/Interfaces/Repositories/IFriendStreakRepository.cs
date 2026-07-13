using Domain.Entities;

namespace Application.Interfaces.Repositories
{
    public interface IFriendStreakRepository
    {
        Task<FriendStreak?> GetAsync(int userAId, int userBId);
        Task<IEnumerable<FriendStreak>> GetForUserAsync(int userId);
        Task<FriendStreak> CreateAsync(FriendStreak friendStreak);
    }
}
