using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class ObjectiveSocialRepository : IObjectiveSocialRepository
    {
        private readonly ApplicationDbContext _context;

        public ObjectiveSocialRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ObjectiveLike?> GetLikeAsync(int objectiveId, int userId)
            => await _context.ObjectiveLikes
                .FirstOrDefaultAsync(l => l.ObjectiveId == objectiveId && l.UserId == userId);

        public async Task<IEnumerable<ObjectiveLike>> GetLikesAsync(IEnumerable<int> objectiveIds)
        {
            var ids = objectiveIds.ToList();
            return await _context.ObjectiveLikes
                .Where(l => ids.Contains(l.ObjectiveId))
                .ToListAsync();
        }

        public void AddLike(ObjectiveLike like) => _context.ObjectiveLikes.Add(like);
        public void RemoveLike(ObjectiveLike like) => _context.ObjectiveLikes.Remove(like);

        public async Task<IEnumerable<ObjectiveComment>> GetCommentsAsync(int objectiveId)
            => await _context.ObjectiveComments
                .Include(c => c.User)
                .Where(c => c.ObjectiveId == objectiveId)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();

        public async Task<Dictionary<int, int>> CountCommentsAsync(IEnumerable<int> objectiveIds)
        {
            var ids = objectiveIds.ToList();
            return await _context.ObjectiveComments
                .Where(c => ids.Contains(c.ObjectiveId))
                .GroupBy(c => c.ObjectiveId)
                .Select(g => new { g.Key, Cantidad = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Cantidad);
        }

        public async Task<ObjectiveComment?> GetCommentAsync(int id)
            => await _context.ObjectiveComments
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == id);

        public void AddComment(ObjectiveComment comment) => _context.ObjectiveComments.Add(comment);
        public void RemoveComment(ObjectiveComment comment) => _context.ObjectiveComments.Remove(comment);

        public async Task<IEnumerable<ObjectiveComment>> GetCommentsByUserAsync(int userId)
            => await _context.ObjectiveComments
                .Include(c => c.User)
                .Where(c => c.UserId == userId)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();

        public async Task<IEnumerable<int>> GetLikedObjectiveIdsAsync(int userId)
            => await _context.ObjectiveLikes
                .Where(l => l.UserId == userId)
                .Select(l => l.ObjectiveId)
                .ToListAsync();
    }
}
