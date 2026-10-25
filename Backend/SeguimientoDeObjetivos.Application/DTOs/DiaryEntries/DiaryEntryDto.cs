using Application.Common;
using System.Text.Json.Serialization;
namespace Application.DTOs.DiaryEntries
{
    public class DiaryEntryDto
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime EntryDate { get; set; }
        public int UserId { get; set; }
        [JsonConverter(typeof(InstanteUtcJsonConverter))]
        public DateTime CreatedAt { get; set; }
        [JsonConverter(typeof(InstanteUtcNullableJsonConverter))]
        public DateTime? UpdatedAt { get; set; }
    }
}
