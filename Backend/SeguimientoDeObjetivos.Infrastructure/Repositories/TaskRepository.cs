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

            // Notifications.TaskId no cascadea (Users ya cascadea a Tasks y a
            // Notifications; una cascada mas daria multiples rutas y SQL Server
            // la rechaza), asi que hay que limpiarlas a mano o el DELETE choca
            // contra la FK. Una notificacion de una tarea que ya no existe no
            // tiene nada que mostrar, asi que se va con ella.
            var notifications = await _context.Notifications
                .Where(n => n.TaskId == id)
                .ToListAsync();
            _context.Notifications.RemoveRange(notifications);

            _context.Tasks.Remove(task);
            return true;
        }
    }
}
