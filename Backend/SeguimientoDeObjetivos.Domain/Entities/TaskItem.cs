using System;
using Domain.Enums;

namespace Domain.Entities
{
    public class TaskItem
    {

        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime ScheduledDate { get; set; }
        public TimeSpan? ScheduledTime { get; set; }

        public int? ReminderMinutesBefore { get; set; }
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;
        public TaskItemStatus Status { get; set; } = TaskItemStatus.Pending;
        public bool IsRecurring { get; set; } = false;
        public RecurrenceType RecurrenceType { get; set; } = RecurrenceType.None;
        public int? RepeatEveryWeeks { get; set; }
        public DateTime? EndRepeatDate { get; set; }
        public int UserId { get; set; }
        public int? ObjectiveId { get; set; }
        public int? CategoryId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        
        public User User { get; set; } = null!;
        public Objective? Objective { get; set; }
        public Category? Category { get; set; }

        public ICollection<TaskRepeatDay> TaskRepeatDays { get; set; } = new List<TaskRepeatDay>();


    }
}


