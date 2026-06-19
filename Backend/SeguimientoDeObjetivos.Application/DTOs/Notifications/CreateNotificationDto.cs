using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Notifications
{
    public class CreateNotificationDto
    {
        [Required]
        public int UserId { get; set; }

        public int? TaskId { get; set; }

        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Message { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty;

        public bool SendEmail { get; set; } = false;
    }
}
