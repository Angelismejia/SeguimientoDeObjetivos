using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class BadgeRepository : IBadgeRepository
    {
        private readonly ApplicationDbContext _context;

        public BadgeRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Badge?> GetByIdAsync(int id)
            => await _context.Badges.FindAsync(id);

        public async Task<IEnumerable<Badge>> GetAllAsync()
            => await _context.Badges.ToListAsync();

        public async Task<IEnumerable<Badge>> GetByUserIdAsync(int userId)
            => await _context.UserBadges
                .Where(ub => ub.UserId == userId)
                .Select(ub => ub.Badge)
                .ToListAsync();

        public async Task<bool> AssignToUserAsync(int userId, int badgeId)
        {
            var alreadyEarned = await _context.UserBadges
                .AnyAsync(ub => ub.UserId == userId && ub.BadgeId == badgeId);

            if (alreadyEarned) return false;

            _context.UserBadges.Add(new UserBadge
            {
                UserId = userId,
                BadgeId = badgeId,
                EarnedAt = DateTime.UtcNow
            });

            return true;
        }
    }
}
