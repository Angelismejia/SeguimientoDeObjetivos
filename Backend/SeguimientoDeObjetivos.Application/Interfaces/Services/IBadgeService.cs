using Application.DTOs.Badges;

namespace Application.Interfaces.Services
{
    public interface IBadgeService
    {
        Task<IEnumerable<BadgeDto>> GetAllAsync();
        Task<BadgeDto?> GetByIdAsync(int id);
        Task<IEnumerable<BadgeDto>> GetByUserIdAsync(int userId);
        Task<bool> AssignToUserAsync(int userId, int badgeId);
    }
}
