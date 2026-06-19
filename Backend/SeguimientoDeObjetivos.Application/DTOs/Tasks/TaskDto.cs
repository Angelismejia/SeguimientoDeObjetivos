using Domain.Enums;

namespace Application.DTOs.Tasks
{
    public class TaskDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime ScheduledDate { get; set; }
        public TimeSpan? ScheduledTime { get; set; }
        public int? ReminderMinutesBefore { get; set; }
        public TaskPriority Priority { get; set; }
        public TaskItemStatus Status { get; set; }
        public bool IsRecurring { get; set; }
        public RecurrenceType RecurrenceType { get; set; }
        public int? RepeatEveryWeeks { get; set; }
        public DateTime? EndRepeatDate { get; set; }
        public int UserId { get; set; }
        public int? ObjectiveId { get; set; }
        public int? CategoryId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
