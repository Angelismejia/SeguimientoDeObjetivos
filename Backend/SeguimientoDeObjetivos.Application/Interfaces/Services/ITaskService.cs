using Application.DTOs.Tasks;

namespace Application.Interfaces.Services
{
    public interface ITaskService
    {
        Task<IEnumerable<TaskDto>> GetByUserIdAsync(int userId);
        Task<IEnumerable<TaskDto>> GetByObjectiveIdAsync(int objectiveId);
        Task<TaskDto> GetByIdAsync(int id);
        Task<TaskDto> CreateAsync(int userId, CreateTaskDto dto);
        Task<TaskDto> UpdateAsync(int id, UpdateTaskDto dto);
        Task DeleteAsync(int id);
    }
}
