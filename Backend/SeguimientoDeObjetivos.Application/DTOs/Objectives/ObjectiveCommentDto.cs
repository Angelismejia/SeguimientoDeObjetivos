using System.Text.Json.Serialization;
using Application.Common;

namespace Application.DTOs.Objectives
{
    public class ObjectiveCommentDto
    {
        public int Id { get; set; }
        public int ObjectiveId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? ProfilePhotoUrl { get; set; }
        public string Content { get; set; } = string.Empty;
        [JsonConverter(typeof(InstanteUtcJsonConverter))]
        public DateTime CreatedAt { get; set; }
    }
}
