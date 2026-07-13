using Domain.Entities;

namespace Application.Interfaces.Repositories
{
    public interface IFollowRepository
    {
        Task<Follow?> GetByIdAsync(int id);
        Task<Follow?> GetAsync(int followerId, int followingId);
        Task<IEnumerable<Follow>> GetFollowersAsync(int userId);
        Task<IEnumerable<Follow>> GetFollowingAsync(int userId);
        Task<Follow> CreateAsync(Follow follow);
        Task<bool> DeleteAsync(int followerId, int followingId);
    }
}
