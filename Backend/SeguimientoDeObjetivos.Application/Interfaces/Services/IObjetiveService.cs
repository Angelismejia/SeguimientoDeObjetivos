using Application.DTOs.Objectives;

namespace Application.Interfaces.Services
{
    public interface IObjectiveService
    {
        Task<IEnumerable<ObjectiveDto>> GetByUserIdAsync(int userId);
        Task<ObjectiveDto> GetByIdAsync(int id);
        Task<ObjectiveDto> CreateAsync(int userId, CreateObjectiveDto dto);
        Task<ObjectiveDto> UpdateAsync(int id, UpdateObjectiveDto dto);
        Task<ObjectiveDto> SetPrimaryAsync(int userId, int objectiveId, bool isPrimary);
        Task<ObjectiveDto> SetPrivateAsync(int userId, int objectiveId, bool isPrivate);
        Task DeleteAsync(int id);
    }
}
