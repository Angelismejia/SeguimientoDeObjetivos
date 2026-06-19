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

        public async Task<Objective> CreateAsync(Objective objective)
        {
            _context.Objectives.Add(objective);
            await _context.SaveChangesAsync();
            return objective;
        }

        public async Task<Objective> UpdateAsync(Objective objective)
        {
            _context.Objectives.Update(objective);
            await _context.SaveChangesAsync();
            return objective;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var objective = await _context.Objectives.FindAsync(id);
            if (objective is null) return false;
            _context.Objectives.Remove(objective);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
