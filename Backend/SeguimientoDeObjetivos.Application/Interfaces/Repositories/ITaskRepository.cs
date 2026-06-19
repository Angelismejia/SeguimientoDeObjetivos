using Domain.Entities;

namespace Application.Interfaces.Repositories
{
    public interface ITaskRepository
    {
        Task<TaskItem?> GetByIdAsync(int id);
        Task<IEnumerable<TaskItem>> GetByUserIdAsync(int userId);
        Task<IEnumerable<TaskItem>> GetByObjectiveIdAsync(int objectiveId);
        Task<TaskItem> CreateAsync(TaskItem task);
        Task<TaskItem> UpdateAsync(TaskItem task);
        Task<bool> DeleteAsync(int id);
    }
}
