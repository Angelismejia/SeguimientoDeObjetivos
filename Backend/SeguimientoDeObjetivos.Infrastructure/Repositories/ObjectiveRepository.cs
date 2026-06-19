using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class ObjectiveRepository : IObjectiveRepository
    {
        private readonly ApplicationDbContext _context;

        public ObjectiveRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Objective?> GetByIdAsync(int id)
            => await _context.Objectives.FindAsync(id);

        public async Task<IEnumerable<Objective>> GetByUserIdAsync(int userId)
            => await _context.Objectives
                .Where(o => o.UserId == userId)
                .ToListAsync();

        public Task<Objective> CreateAsync(Objective objective)
        {
            _context.Objectives.Add(objective);
            return Task.FromResult(objective);
        }

        public Task<Objective> UpdateAsync(Objective objective)
        {
            _context.Objectives.Update(objective);
            return Task.FromResult(objective);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var objective = await _context.Objectives.FindAsync(id);
            if (objective is null) return false;
            _context.Objectives.Remove(objective);
            return true;
        }
    }
}
