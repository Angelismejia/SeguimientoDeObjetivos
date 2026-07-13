using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class FriendStreakRepository : IFriendStreakRepository
    {
        private readonly ApplicationDbContext _context;

        public FriendStreakRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<FriendStreak?> GetAsync(int userAId, int userBId)
            => await _context.FriendStreaks
                .FirstOrDefaultAsync(f =>
                    (f.UserAId == userAId && f.UserBId == userBId) ||
                    (f.UserAId == userBId && f.UserBId == userAId));

        public async Task<IEnumerable<FriendStreak>> GetForUserAsync(int userId)
            => await _context.FriendStreaks
                .Include(f => f.UserA)
                .Include(f => f.UserB)
                .Where(f => f.UserAId == userId || f.UserBId == userId)
                .ToListAsync();

        public Task<FriendStreak> CreateAsync(FriendStreak friendStreak)
        {
            _context.FriendStreaks.Add(friendStreak);
            return Task.FromResult(friendStreak);
        }
    }
}
