using Application.Common;
using System.Text.Json.Serialization;
namespace Application.DTOs.Categories
{
    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Color { get; set; }
        public string? Icon { get; set; }
        public bool IsDefault { get; set; }
        public int UserId { get; set; }
        [JsonConverter(typeof(InstanteUtcJsonConverter))]
        public DateTime CreatedAt { get; set; }
    }
}
