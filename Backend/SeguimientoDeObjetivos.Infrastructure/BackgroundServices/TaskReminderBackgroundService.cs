using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundServices
{
    public class TaskReminderBackgroundService : BackgroundService
    {
        private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IAppClock _appClock;
        private readonly ILogger<TaskReminderBackgroundService> _logger;

        public TaskReminderBackgroundService(
            IServiceScopeFactory scopeFactory,
            IAppClock appClock,
            ILogger<TaskReminderBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _appClock = appClock;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(CheckInterval);
            do
            {
                try
                {
                    await CheckDueTasksAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking due task reminders");
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }

        private async Task CheckDueTasksAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // DateTime.Today / DateTime.Now aca eran la hora del servidor (UTC en
            // Azure), no la del usuario que programo la tarea con ScheduledTime en
            // su propia hora local -> los recordatorios llegaban desfasados.
            var today = _appClock.Today;
            var now = _appClock.Now.TimeOfDay;

            // CreatedAt se guarda en UTC (ver Notification.CreatedAt), asi que para
            // "ya se lo notifique hoy" comparamos contra el rango UTC del dia local,
            // no contra .Date directamente (eso mezclaria UTC con hora local otra vez).
            var todayStartUtc = _appClock.ToUtc(today);
            var todayEndUtc = todayStartUtc.AddDays(1);

            var dueTasks = await db.Tasks
                .Where(t => t.ScheduledDate.Date == today
                    && t.ScheduledTime != null
                    && t.ScheduledTime <= now
                    && t.Status != TaskItemStatus.Completed
                    && t.Status != TaskItemStatus.Skipped)
                .ToListAsync(stoppingToken);

            if (dueTasks.Count == 0) return;

            foreach (var task in dueTasks)
            {
                var alreadyNotified = await db.Notifications.AnyAsync(n =>
                    n.TaskId == task.Id
                    && n.Type == "TaskReminder"
                    && n.CreatedAt >= todayStartUtc && n.CreatedAt < todayEndUtc, stoppingToken);

                if (alreadyNotified) continue;

                db.Notifications.Add(new Notification
                {
                    UserId = task.UserId,
                    TaskId = task.Id,
                    Title = "Es hora de tu tarea",
                    Message = $"\"{task.Title}\" estaba programada para ahora.",
                    Type = "TaskReminder",
                    SentAt = DateTime.UtcNow
                });
            }

            await db.SaveChangesAsync(stoppingToken);
        }
    }
}
