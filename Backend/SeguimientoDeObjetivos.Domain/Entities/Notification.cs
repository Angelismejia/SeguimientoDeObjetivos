using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    public class Notification
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? TaskId { get; set; }
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string Type { get; set; } = null!;
        public bool IsRead { get; set; } = false;
        public bool SendEmail { get; set; } = false;
        public DateTime? SentAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; } = null!;

        [ForeignKey("TaskId")]
        public TaskItem? TaskItem { get; set; }
    }
}




