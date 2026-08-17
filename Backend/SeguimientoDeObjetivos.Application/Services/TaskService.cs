using Application.DTOs.Tasks;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;

namespace Application.Services
{
    public class TaskService : ITaskService
    {
        private readonly ITaskRepository _taskRepository;
        private readonly ITaskCompletionRepository _completionRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBadgeAwardService _badgeAwardService;

        public TaskService(
            ITaskRepository taskRepository,
            ITaskCompletionRepository completionRepository,
            IUnitOfWork unitOfWork,
            IBadgeAwardService badgeAwardService)
        {
            _taskRepository = taskRepository;
            _completionRepository = completionRepository;
            _unitOfWork = unitOfWork;
            _badgeAwardService = badgeAwardService;
        }

        public async Task<IEnumerable<TaskDto>> GetByUserIdAsync(int userId)
        {
            var tasks = await _taskRepository.GetByUserIdAsync(userId);

            // Una sola consulta para todos los dias completados del usuario, en vez
            // de una por tarea: es lo que alimenta la racha y el calendario.
            var completions = await _completionRepository.GetByUserIdAsync(userId);
            var porTarea = completions
                .GroupBy(c => c.TaskId)
                .ToDictionary(g => g.Key, g => g.Select(c => c.Date).OrderBy(d => d).ToList());

            return tasks.Select(t => ToDto(
                t,
                porTarea.TryGetValue(t.Id, out var dias) ? dias : null));
        }

        // Marca o desmarca una tarea recurrente en un dia concreto.
        //
        // Las recurrentes son una sola fila con una unica ScheduledDate, asi que su
        // estado no puede describir mas de un dia: antes, completar hoy pisaba lo de
        // ayer. Cada dia completado pasa a ser una fila propia en TaskCompletions.
        public async Task SetCompletionAsync(int taskId, SetCompletionDto dto)
        {
            var task = await _taskRepository.GetByIdAsync(taskId)
                ?? throw new NotFoundException("Task", taskId);

            if (!task.IsRecurring)
                throw new InvalidOperationException("Solo las tareas recurrentes llevan historial por dia.");

            var dia = dto.Date.Date;
            var existente = await _completionRepository.GetAsync(taskId, dia);

            if (dto.Completed && existente is null)
            {
                await _completionRepository.CreateAsync(new TaskCompletion
                {
                    TaskId = taskId,
                    Date = dia,
                    CompletedAt = DateTime.UtcNow
                });
            }
            else if (!dto.Completed && existente is not null)
            {
                await _completionRepository.DeleteAsync(taskId, dia);
            }

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<IEnumerable<TaskDto>> GetByObjectiveIdAsync(int objectiveId)
        {
            var tasks = await _taskRepository.GetByObjectiveIdAsync(objectiveId);
            return tasks.Select(t => ToDto(t));
        }

        public async Task<TaskDto> GetByIdAsync(int id)
        {
            var task = await _taskRepository.GetByIdAsync(id);
            if (task is null) throw new NotFoundException("Task", id);
            return ToDto(task);
        }

        public async Task<TaskDto> CreateAsync(int userId, CreateTaskDto dto)
        {
            var task = new TaskItem
            {
                Title = dto.Title,
                Description = dto.Description,
                Emoji = dto.Emoji,
                Color = dto.Color,
                ScheduledDate = dto.ScheduledDate,
                ScheduledTime = dto.ScheduledTime,
                EndTime = dto.EndTime,
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

        public async Task<TaskDto> UpdateAsync(int id, UpdateTaskDto dto)
        {
            var task = await _taskRepository.GetByIdAsync(id);
            if (task is null) throw new NotFoundException("Task", id);

            task.Title = dto.Title;
            task.Description = dto.Description;
            task.Emoji = dto.Emoji;
            task.Color = dto.Color;
            task.ScheduledDate = dto.ScheduledDate;
            task.ScheduledTime = dto.ScheduledTime;
            task.EndTime = dto.EndTime;
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

            var justCompleted = dto.Status == TaskItemStatus.Completed && task.CompletedAt is null;
            if (justCompleted)
                task.CompletedAt = DateTime.UtcNow;

            await _taskRepository.UpdateAsync(task);
            await _unitOfWork.SaveChangesAsync();

            if (justCompleted)
                await _badgeAwardService.CheckAndAwardAsync(task.UserId);

            return ToDto(task);
        }

        public async Task DeleteAsync(int id)
        {
            var deleted = await _taskRepository.DeleteAsync(id);
            if (!deleted) throw new NotFoundException("Task", id);
            await _unitOfWork.SaveChangesAsync();
        }

        private static TaskDto ToDto(TaskItem t, List<DateTime>? completedDates = null) => new()
        {
            CompletedDates = completedDates ?? new List<DateTime>(),
            Id = t.Id,
            Title = t.Title,
            Description = t.Description,
            Emoji = t.Emoji,
            Color = t.Color,
            ScheduledDate = t.ScheduledDate,
            ScheduledTime = t.ScheduledTime,
            EndTime = t.EndTime,
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
