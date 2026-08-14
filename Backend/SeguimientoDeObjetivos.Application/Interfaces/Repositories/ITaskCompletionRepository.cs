using Domain.Entities;

namespace Application.Interfaces.Repositories
{
    public interface ITaskCompletionRepository
    {
        Task<IEnumerable<TaskCompletion>> GetByUserIdAsync(int userId);
        Task<IEnumerable<TaskCompletion>> GetByTaskIdAsync(int taskId);
        Task<TaskCompletion?> GetAsync(int taskId, DateTime date);
        Task<TaskCompletion> CreateAsync(TaskCompletion completion);
        Task<bool> DeleteAsync(int taskId, DateTime date);
    }
}
