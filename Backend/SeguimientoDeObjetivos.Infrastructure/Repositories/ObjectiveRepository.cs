using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Enums;
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

        // Los publicos de varias personas, logrados primero y luego los mas
        // recientes. El orden y el limite van en la consulta, no en memoria.
        public async Task<IEnumerable<Objective>> GetPublicFeedAsync(IEnumerable<int> userIds, int limit)
        {
            var ids = userIds.ToList();
            return await _context.Objectives
                .Include(o => o.User)
                .Where(o => ids.Contains(o.UserId) && !o.IsPrivate)
                .OrderBy(o => o.Status == ObjectiveStatus.Completed ? 0
                    : o.Status == ObjectiveStatus.InProgress ? 1
                    : o.Status == ObjectiveStatus.Pending ? 2 : 3)
                .ThenByDescending(o => o.UpdatedAt ?? o.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

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

            // Tasks.ObjectiveId no cascadea, asi que sin esto el DELETE choca
            // contra la FK en cuanto el objetivo tenga tareas. Se sueltan en vez
            // de borrarlas: ObjectiveId es opcional (la app admite tareas sin
            // objetivo) y el dialogo de borrado no avisa de perder tareas.
            var tasks = await _context.Tasks
                .Where(t => t.ObjectiveId == id)
                .ToListAsync();
            foreach (var task in tasks)
                task.ObjectiveId = null;

            _context.Objectives.Remove(objective);
            return true;
        }
    }
}
