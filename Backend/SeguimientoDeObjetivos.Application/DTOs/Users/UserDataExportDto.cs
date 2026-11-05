using Application.Common;
using System.Text.Json.Serialization;
using Application.DTOs.Badges;
using Application.DTOs.Categories;
using Application.DTOs.DiaryEntries;
using Application.DTOs.Objectives;
using Application.DTOs.Tasks;

namespace Application.DTOs.Users
{
    public class UserDataExportDto
    {
        [JsonConverter(typeof(InstanteUtcJsonConverter))]
        public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
        public UserDto User { get; set; } = null!;
        public IEnumerable<CategoryDto> Categories { get; set; } = Array.Empty<CategoryDto>();
        public IEnumerable<ObjectiveDto> Objectives { get; set; } = Array.Empty<ObjectiveDto>();
        public IEnumerable<TaskDto> Tasks { get; set; } = Array.Empty<TaskDto>();
        public IEnumerable<DiaryEntryDto> DiaryEntries { get; set; } = Array.Empty<DiaryEntryDto>();
        public IEnumerable<BadgeDto> Badges { get; set; } = Array.Empty<BadgeDto>();

        /// <summary>Comentarios que la persona escribio en logros de otros (o en los suyos).</summary>
        public IEnumerable<ObjectiveCommentDto> CommentsWritten { get; set; } = Array.Empty<ObjectiveCommentDto>();

        /// <summary>Ids de los objetivos a los que la persona les dio me gusta.</summary>
        public IEnumerable<int> LikedObjectiveIds { get; set; } = Array.Empty<int>();
    }
}
