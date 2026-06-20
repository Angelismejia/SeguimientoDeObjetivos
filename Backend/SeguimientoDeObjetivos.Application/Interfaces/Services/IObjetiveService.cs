using Application.DTOs.Objectives;

namespace Application.Interfaces.Services
{
    public interface IObjectiveService
    {
        Task<IEnumerable<ObjectiveDto>> GetByUserIdAsync(int userId);
        Task<ObjectiveDto> GetByIdAsync(int id);
        Task<ObjectiveDto> CreateAsync(int userId, CreateObjectiveDto dto);
        Task<ObjectiveDto> UpdateAsync(int id, UpdateObjectiveDto dto);
        Task DeleteAsync(int id);
    }
}
