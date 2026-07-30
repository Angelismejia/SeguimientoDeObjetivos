using Application.DTOs.Objectives;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;

namespace Application.Services
{
    public class ObjectiveService : IObjectiveService
    {
        private readonly IObjectiveRepository _objectiveRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBadgeAwardService _badgeAwardService;

        public ObjectiveService(IObjectiveRepository objectiveRepository, IUnitOfWork unitOfWork, IBadgeAwardService badgeAwardService)
        {
            _objectiveRepository = objectiveRepository;
            _unitOfWork = unitOfWork;
            _badgeAwardService = badgeAwardService;
        }

        public async Task<IEnumerable<ObjectiveDto>> GetByUserIdAsync(int userId)
        {
            var objectives = await _objectiveRepository.GetByUserIdAsync(userId);
            return objectives.Select(ToDto);
        }

        public async Task<ObjectiveDto> GetByIdAsync(int id)
        {
            var objective = await _objectiveRepository.GetByIdAsync(id);
            if (objective is null) throw new NotFoundException("Objective", id);
            return ToDto(objective);
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

        public async Task<ObjectiveDto> UpdateAsync(int id, UpdateObjectiveDto dto)
        {
            var objective = await _objectiveRepository.GetByIdAsync(id);
            if (objective is null) throw new NotFoundException("Objective", id);

            var justCompleted = dto.Status == ObjectiveStatus.Completed && objective.Status != ObjectiveStatus.Completed;

            objective.Title = dto.Title;
            objective.Description = dto.Description;
            objective.StartDate = dto.StartDate;
            objective.EndDate = dto.EndDate;
            objective.Status = dto.Status;
            objective.ProgressPercentage = dto.ProgressPercentage;
            objective.CategoryId = dto.CategoryId;
            objective.UpdatedAt = DateTime.UtcNow;

            await _objectiveRepository.UpdateAsync(objective);
            await _unitOfWork.SaveChangesAsync();

            if (justCompleted)
                await _badgeAwardService.CheckAndAwardAsync(objective.UserId);

            return ToDto(objective);
        }

        public async Task<ObjectiveDto> SetPrimaryAsync(int userId, int objectiveId, bool isPrimary)
        {
            var objectives = (await _objectiveRepository.GetByUserIdAsync(userId)).ToList();
            var target = objectives.FirstOrDefault(o => o.Id == objectiveId);
            if (target is null) throw new NotFoundException("Objective", objectiveId);

            // Solo puede haber un objetivo principal por usuario: si se marca este,
            // se desmarca cualquier otro que lo tuviera.
            if (isPrimary)
            {
                foreach (var other in objectives.Where(o => o.Id != objectiveId && o.IsPrimary))
                {
                    other.IsPrimary = false;
                    await _objectiveRepository.UpdateAsync(other);
                }
            }

            target.IsPrimary = isPrimary;
            await _objectiveRepository.UpdateAsync(target);
            await _unitOfWork.SaveChangesAsync();

            return ToDto(target);
        }

        public async Task<ObjectiveDto> SetPrivateAsync(int userId, int objectiveId, bool isPrivate)
        {
            var objectives = await _objectiveRepository.GetByUserIdAsync(userId);
            var target = objectives.FirstOrDefault(o => o.Id == objectiveId);
            if (target is null) throw new NotFoundException("Objective", objectiveId);

            target.IsPrivate = isPrivate;
            await _objectiveRepository.UpdateAsync(target);
            await _unitOfWork.SaveChangesAsync();

            return ToDto(target);
        }

        public async Task DeleteAsync(int id)
        {
            var deleted = await _objectiveRepository.DeleteAsync(id);
            if (!deleted) throw new NotFoundException("Objective", id);
            await _unitOfWork.SaveChangesAsync();
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
            IsPrimary = o.IsPrimary,
            IsPrivate = o.IsPrivate,
            UserId = o.UserId,
            CategoryId = o.CategoryId,
            CreatedAt = o.CreatedAt,
            UpdatedAt = o.UpdatedAt
        };
    }
}
