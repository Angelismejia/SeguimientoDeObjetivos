using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services
{
    public class BadgeAwardService : IBadgeAwardService
    {
        private readonly IBadgeRepository _badgeRepository;
        private readonly ITaskRepository _taskRepository;
        private readonly IObjectiveRepository _objectiveRepository;
        private readonly IUnitOfWork _unitOfWork;

        // BadgeType (tal como se guarda en la tabla Badges) -> condición para ganarla.
        // tasksCompleted, objectivesCompleted y currentStreak son los tres números
        // de progreso que calculamos una vez y reutilizamos para todas las insignias.
        private static readonly Dictionary<string, Func<int, int, int, bool>> Criteria = new()
        {
            ["first_task"] = (tasksCompleted, _, _) => tasksCompleted >= 1,
            ["tasks_10"] = (tasksCompleted, _, _) => tasksCompleted >= 10,
            ["tasks_50"] = (tasksCompleted, _, _) => tasksCompleted >= 50,
            ["first_objective"] = (_, objectivesCompleted, _) => objectivesCompleted >= 1,
            ["objectives_5"] = (_, objectivesCompleted, _) => objectivesCompleted >= 5,
            ["streak_3"] = (_, _, currentStreak) => currentStreak >= 3,
            ["streak_7"] = (_, _, currentStreak) => currentStreak >= 7,
            ["streak_30"] = (_, _, currentStreak) => currentStreak >= 30,
        };

        public BadgeAwardService(
            IBadgeRepository badgeRepository,
            ITaskRepository taskRepository,
            IObjectiveRepository objectiveRepository,
            IUnitOfWork unitOfWork)
        {
            _badgeRepository = badgeRepository;
            _taskRepository = taskRepository;
            _objectiveRepository = objectiveRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task CheckAndAwardAsync(int userId)
        {
            var tasks = (await _taskRepository.GetByUserIdAsync(userId)).ToList();
            var objectives = await _objectiveRepository.GetByUserIdAsync(userId);

            var tasksCompleted = tasks.Count(t => t.Status == TaskItemStatus.Completed);
            var objectivesCompleted = objectives.Count(o => o.Status == ObjectiveStatus.Completed);
            var currentStreak = ComputeCurrentStreak(tasks);

            var badges = await _badgeRepository.GetAllAsync();
            var awardedAny = false;

            foreach (var badge in badges)
            {
                if (!Criteria.TryGetValue(badge.BadgeType, out var isEarned)) continue;
                if (!isEarned(tasksCompleted, objectivesCompleted, currentStreak)) continue;

                var awarded = await _badgeRepository.AssignToUserAsync(userId, badge.Id);
                if (awarded) awardedAny = true;
            }

            if (awardedAny) await _unitOfWork.SaveChangesAsync();
        }

        // Misma regla que usa el frontend: días consecutivos con al menos una
        // tarea completada, contando desde hoy (o desde ayer si hoy todavía no
        // se completó ninguna, para no cortar la racha antes de que termine el día).
        private static int ComputeCurrentStreak(IEnumerable<TaskItem> tasks)
        {
            var completedDays = tasks
                .Where(t => t.Status == TaskItemStatus.Completed)
                .Select(t => t.ScheduledDate.Date)
                .ToHashSet();

            var cursor = DateTime.UtcNow.Date;
            if (!completedDays.Contains(cursor)) cursor = cursor.AddDays(-1);

            var streak = 0;
            while (completedDays.Contains(cursor))
            {
                streak++;
                cursor = cursor.AddDays(-1);
            }
            return streak;
        }
    }
}
