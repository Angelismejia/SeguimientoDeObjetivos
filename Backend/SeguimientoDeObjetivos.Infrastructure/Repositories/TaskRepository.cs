using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class TaskRepository : ITaskRepository
    {
        private readonly ApplicationDbContext _context;

        public TaskRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<TaskItem?> GetByIdAsync(int id)
            => await _context.Tasks.FindAsync(id);

        public async Task<IEnumerable<TaskItem>> GetByUserIdAsync(int userId)
            => await _context.Tasks
                .Where(t => t.UserId == userId)
                .ToListAsync();

        public async Task<IEnumerable<TaskItem>> GetByObjectiveIdAsync(int objectiveId)
            => await _context.Tasks
                .Where(t => t.ObjectiveId == objectiveId)
                .ToListAsync();

        public Task<TaskItem> CreateAsync(TaskItem task)
        {
            _context.Tasks.Add(task);
            return Task.FromResult(task);
        }

        public Task<TaskItem> UpdateAsync(TaskItem task)
        {
            _context.Tasks.Update(task);
            return Task.FromResult(task);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task is null) return false;
            _context.Tasks.Remove(task);
            return true;
        }
    }
}
