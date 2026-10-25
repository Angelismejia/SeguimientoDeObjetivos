using Application.Common;
using System.Text.Json.Serialization;
namespace Application.DTOs.Users
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        [JsonConverter(typeof(InstanteUtcJsonConverter))]
        public DateTime CreatedAt { get; set; }
        [JsonConverter(typeof(InstanteUtcNullableJsonConverter))]
        public DateTime? UpdatedAt { get; set; }
        public string ProfilePhotoUrl { get; set; } = string.Empty;
        public bool AllowFollows { get; set; }
    }
}