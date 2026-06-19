using Application.DTOs.Objectives;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;

namespace Application.Services
{
    public class ObjectiveService : IObjectiveService
    {
        private readonly IObjectiveRepository _objectiveRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ObjectiveService(IObjectiveRepository objectiveRepository, IUnitOfWork unitOfWork)
        {
            _objectiveRepository = objectiveRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<ObjectiveDto>> GetByUserIdAsync(int userId)
        {
            var objectives = await _objectiveRepository.GetByUserIdAsync(userId);
            return objectives.Select(ToDto);
        }

        public async Task<ObjectiveDto?> GetByIdAsync(int id)
        {
            var objective = await _objectiveRepository.GetByIdAsync(id);
            return objective is null ? null : ToDto(objective);
        }

        public async Task<ObjectiveDto> CreateAsync(int userId, CreateObjectiveDto dto)
        {
            var objective = new Objective
            {
                Title = dto.Title,
                Description = dto.Description,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                CategoryId = dto.CategoryId,
                UserId = userId
            };

            var created = await _objectiveRepository.CreateAsync(objective);
            await _unitOfWork.SaveChangesAsync();
            return ToDto(created);
        }

        public async Task<ObjectiveDto?> UpdateAsync(int id, UpdateObjectiveDto dto)
        {
            var objective = await _objectiveRepository.GetByIdAsync(id);
            if (objective is null) return null;

            objective.Title = dto.Title;
            objective.Description = dto.Description;
            objective.StartDate = dto.StartDate;
            objective.EndDate = dto.EndDate;
            objective.Status = dto.Status;
            objective.ProgressPercentage = dto.ProgressPercentage;
            objective.CategoryId = dto.CategoryId;
            objective.UpdatedAt = DateTime.UtcNow;

            var updated = await _objectiveRepository.UpdateAsync(objective);
            await _unitOfWork.SaveChangesAsync();
            return ToDto(updated);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var deleted = await _objectiveRepository.DeleteAsync(id);
            if (!deleted) return false;
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        private static ObjectiveDto ToDto(Objective o) => new()
        {
            Id = o.Id,
            Title = o.Title,
            Description = o.Description,
            StartDate = o.StartDate,
            EndDate = o.EndDate,
            Status = o.Status,
            ProgressPercentage = o.ProgressPercentage,
            UserId = o.UserId,
            CategoryId = o.CategoryId,
            CreatedAt = o.CreatedAt,
            UpdatedAt = o.UpdatedAt
        };
    }
}
