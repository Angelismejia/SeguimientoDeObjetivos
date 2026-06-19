using Application.DTOs.Objectives;          
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;   
using Domain.Entities;  
namespace Application.Services


{
    public class ObjectiveService : IObjectiveService
    {
        private readonly IObjectiveRepository _objectiveRepository;

        public ObjectiveService(IObjectiveRepository objectiveRepository)
        {
            _objectiveRepository = objectiveRepository;
        }

        // devuelve todos los objetivos de un usuario
        public async Task<IEnumerable<ObjectiveDto>> GetByUserIdAsync(int userId)
        {
            var objectives = await _objectiveRepository.GetByUserIdAsync(userId);
            return objectives.Select(ToDto);
        }

        // devuelve un objetivo por id
        public async Task<ObjectiveDto?> GetByIdAsync(int id)
        {
            var objective = await _objectiveRepository.GetByIdAsync(id);
            return objective is null ? null : ToDto(objective);
        }

        // crea un objetivo nuevo
        // el Status arranca en "Pending" y ProgressPercentage en 0 por defecto (definido en la entidad)
        public async Task<ObjectiveDto> CreateAsync(int userId, CreateObjectiveDto dto)
        {
            var objective = new Objective
            {
                Title = dto.Title,
                Description = dto.Description,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                CategoryId = dto.CategoryId,
                UserId = userId // viene del controller, no del DTO
            };

            var created = await _objectiveRepository.CreateAsync(objective);
            return ToDto(created);
        }

        // actualiza un objetivo existente
        // aquí sí se puede cambiar el Status y ProgressPercentage porque el usuario tiene control sobre eso
        public async Task<ObjectiveDto?> UpdateAsync(int id, UpdateObjectiveDto dto)
        {
            var objective = await _objectiveRepository.GetByIdAsync(id);
            if (objective is null) return null;

            objective.Title = dto.Title;
            objective.Description = dto.Description;
            objective.StartDate = dto.StartDate;
            objective.EndDate = dto.EndDate;
            objective.Status = dto.Status;                       // puede cambiar a "InProgress", "Completed", etc.
            objective.ProgressPercentage = dto.ProgressPercentage; // porcentaje actualizable manualmente
            objective.CategoryId = dto.CategoryId;
            objective.UpdatedAt = DateTime.UtcNow;               // marca cuándo fue modificado

            var updated = await _objectiveRepository.UpdateAsync(objective);
            return ToDto(updated);
        }

        // elimina un objetivo
        public async Task<bool> DeleteAsync(int id)
        {
            return await _objectiveRepository.DeleteAsync(id);
        }

        // convierte la entidad Objective al DTO de respuesta
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
