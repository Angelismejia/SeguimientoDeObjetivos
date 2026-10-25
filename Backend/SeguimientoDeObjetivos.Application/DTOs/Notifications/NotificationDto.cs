using Application.Common;
using System.Text.Json.Serialization;
namespace Application.DTOs.Notifications
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? TaskId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public bool SendEmail { get; set; }
        [JsonConverter(typeof(InstanteUtcNullableJsonConverter))]
        public DateTime? SentAt { get; set; }
        [JsonConverter(typeof(InstanteUtcJsonConverter))]
        public DateTime CreatedAt { get; set; }
    }
}
