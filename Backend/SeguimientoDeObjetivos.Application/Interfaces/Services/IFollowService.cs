using Application.DTOs.Follows;

namespace Application.Interfaces.Services
{
    public interface IFollowService
    {
        Task<IEnumerable<UserSummaryDto>> GetFollowersAsync(int userId);
        Task<IEnumerable<UserSummaryDto>> GetFollowingAsync(int userId);
        Task<FollowDto> CreateAsync(int followerId, CreateFollowDto dto);
        Task DeleteAsync(int followerId, int followingId);
    }
}
