using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class FollowRepository : IFollowRepository
    {
        private readonly ApplicationDbContext _context;

        public FollowRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Follow?> GetByIdAsync(int id)
            => await _context.Follows.FindAsync(id);

        public async Task<Follow?> GetAsync(int followerId, int followingId)
            => await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowingId == followingId);

        public async Task<IEnumerable<Follow>> GetFollowersAsync(int userId)
            => await _context.Follows
                .Include(f => f.Follower)
                .Where(f => f.FollowingId == userId)
                .ToListAsync();

        public async Task<IEnumerable<Follow>> GetFollowingAsync(int userId)
            => await _context.Follows
                .Include(f => f.Following)
                .Where(f => f.FollowerId == userId)
                .ToListAsync();

        public Task<Follow> CreateAsync(Follow follow)
        {
            _context.Follows.Add(follow);
            return Task.FromResult(follow);
        }

        public async Task<bool> DeleteAsync(int followerId, int followingId)
        {
            var follow = await GetAsync(followerId, followingId);
            if (follow is null) return false;
            _context.Follows.Remove(follow);
            return true;
        }
    }
}
