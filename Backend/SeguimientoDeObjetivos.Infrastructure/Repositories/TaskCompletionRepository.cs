using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class TaskCompletionRepository : ITaskCompletionRepository
    {
        private readonly ApplicationDbContext _context;

        public TaskCompletionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // Se filtra por el dueño de la tarea, no por la tarea, para poder traer de
        // una sola vez todos los dias completados del usuario y calcular la racha
        // sin una consulta por tarea.
        public async Task<IEnumerable<TaskCompletion>> GetByUserIdAsync(int userId)
            => await _context.TaskCompletions
                .Where(c => c.Task.UserId == userId)
                .ToListAsync();

        public async Task<IEnumerable<TaskCompletion>> GetByTaskIdAsync(int taskId)
            => await _context.TaskCompletions
                .Where(c => c.TaskId == taskId)
                .ToListAsync();

        public async Task<TaskCompletion?> GetAsync(int taskId, DateTime date)
            => await _context.TaskCompletions
                .FirstOrDefaultAsync(c => c.TaskId == taskId && c.Date == date.Date);

        public Task<TaskCompletion> CreateAsync(TaskCompletion completion)
        {
            completion.Date = completion.Date.Date;
            _context.TaskCompletions.Add(completion);
            return Task.FromResult(completion);
        }

        public async Task<bool> DeleteAsync(int taskId, DateTime date)
        {
            var completion = await GetAsync(taskId, date);
            if (completion is null) return false;

            _context.TaskCompletions.Remove(completion);
            return true;
        }
    }
}
