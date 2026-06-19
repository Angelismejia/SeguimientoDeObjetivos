using Application.DTOs.Badges;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;

namespace Application.Services
{
    public class BadgeService : IBadgeService
    {
        private readonly IBadgeRepository _badgeRepository;
        private readonly IUnitOfWork _unitOfWork;

        public BadgeService(IBadgeRepository badgeRepository, IUnitOfWork unitOfWork)
        {
            _badgeRepository = badgeRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<BadgeDto>> GetAllAsync()
        {
            var badges = await _badgeRepository.GetAllAsync();
            return badges.Select(ToDto);
        }

        public async Task<BadgeDto?> GetByIdAsync(int id)
        {
            var badge = await _badgeRepository.GetByIdAsync(id);
            return badge is null ? null : ToDto(badge);
        }

        public async Task<IEnumerable<BadgeDto>> GetByUserIdAsync(int userId)
        {
            var badges = await _badgeRepository.GetByUserIdAsync(userId);
            return badges.Select(ToDto);
        }

        public async Task<bool> AssignToUserAsync(int userId, int badgeId)
        {
            var assigned = await _badgeRepository.AssignToUserAsync(userId, badgeId);
            if (!assigned) return false;
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        private static BadgeDto ToDto(Domain.Entities.Badge b) => new()
        {
            Id = b.Id,
            Name = b.Name,
            Description = b.Description,
            BadgeType = b.BadgeType,
            Icon = b.Icon,
            CreatedAt = b.CreatedAt
        };
    }
}
