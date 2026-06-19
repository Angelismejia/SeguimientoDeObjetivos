using Application.DTOs.Tasks;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services
{
    public class TaskService : ITaskService
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IUnitOfWork _unitOfWork;

        public TaskService(ITaskRepository taskRepository, IUnitOfWork unitOfWork)
        {
            _taskRepository = taskRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<TaskDto>> GetByUserIdAsync(int userId)
        {
            var tasks = await _taskRepository.GetByUserIdAsync(userId);
            return tasks.Select(ToDto);
        }

        public async Task<IEnumerable<TaskDto>> GetByObjectiveIdAsync(int objectiveId)
        {
            var tasks = await _taskRepository.GetByObjectiveIdAsync(objectiveId);
            return tasks.Select(ToDto);
        }

        public async Task<TaskDto?> GetByIdAsync(int id)
        {
            var task = await _taskRepository.GetByIdAsync(id);
            return task is null ? null : ToDto(task);
        }

        public async Task<TaskDto> CreateAsync(int userId, CreateTaskDto dto)
        {
            var task = new TaskItem
            {
                Title = dto.Title,
                Description = dto.Description,
                ScheduledDate = dto.ScheduledDate,
                ScheduledTime = dto.ScheduledTime,
                ReminderMinutesBefore = dto.ReminderMinutesBefore,
                Priority = dto.Priority,
                IsRecurring = dto.IsRecurring,
                RecurrenceType = dto.RecurrenceType ?? RecurrenceType.None,
                RepeatEveryWeeks = dto.RepeatEveryWeeks,
                EndRepeatDate = dto.EndRepeatDate,
                ObjectiveId = dto.ObjectiveId,
                CategoryId = dto.CategoryId,
                UserId = userId
            };

            var created = await _taskRepository.CreateAsync(task);
            await _unitOfWork.SaveChangesAsync();
            return ToDto(created);
        }

        public async Task<TaskDto?> UpdateAsync(int id, UpdateTaskDto dto)
        {
            var task = await _taskRepository.GetByIdAsync(id);
            if (task is null) return null;

            task.Title = dto.Title;
            task.Description = dto.Description;
            task.ScheduledDate = dto.ScheduledDate;
            task.ScheduledTime = dto.ScheduledTime;
            task.ReminderMinutesBefore = dto.ReminderMinutesBefore;
            task.Priority = dto.Priority;
            task.Status = dto.Status;
            task.IsRecurring = dto.IsRecurring;
            task.RecurrenceType = dto.RecurrenceType ?? RecurrenceType.None;
            task.RepeatEveryWeeks = dto.RepeatEveryWeeks;
            task.EndRepeatDate = dto.EndRepeatDate;
            task.ObjectiveId = dto.ObjectiveId;
            task.CategoryId = dto.CategoryId;
            task.UpdatedAt = DateTime.UtcNow;

            if (dto.Status == TaskItemStatus.Completed && task.CompletedAt is null)
                task.CompletedAt = DateTime.UtcNow;

            var updated = await _taskRepository.UpdateAsync(task);
            await _unitOfWork.SaveChangesAsync();
            return ToDto(updated);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var deleted = await _taskRepository.DeleteAsync(id);
            if (!deleted) return false;
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        private static TaskDto ToDto(TaskItem t) => new()
        {
            Id = t.Id,
            Title = t.Title,
            Description = t.Description,
            ScheduledDate = t.ScheduledDate,
            ScheduledTime = t.ScheduledTime,
            ReminderMinutesBefore = t.ReminderMinutesBefore,
            Priority = t.Priority,
            Status = t.Status,
            IsRecurring = t.IsRecurring,
            RecurrenceType = t.RecurrenceType,
            RepeatEveryWeeks = t.RepeatEveryWeeks,
            EndRepeatDate = t.EndRepeatDate,
            UserId = t.UserId,
            ObjectiveId = t.ObjectiveId,
            CategoryId = t.CategoryId,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt,
            CompletedAt = t.CompletedAt
        };
    }
}
