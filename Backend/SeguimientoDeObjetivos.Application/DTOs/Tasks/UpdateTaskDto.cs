using Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Tasks
{
    public class UpdateTaskDto
    {
        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        public DateTime ScheduledDate { get; set; }

        public TimeSpan? ScheduledTime { get; set; }

        [Range(0, 1440)]
        public int? ReminderMinutesBefore { get; set; }

        public TaskPriority Priority { get; set; }

        public TaskItemStatus Status { get; set; }

        public bool IsRecurring { get; set; }

        public RecurrenceType? RecurrenceType { get; set; }

        [Range(1, 52)]
        public int? RepeatEveryWeeks { get; set; }

        public DateTime? EndRepeatDate { get; set; }
        public int? ObjectiveId { get; set; }
        public int? CategoryId { get; set; }
    }
}
