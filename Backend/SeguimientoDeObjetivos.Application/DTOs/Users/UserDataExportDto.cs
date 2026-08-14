using Application.DTOs.Badges;
using Application.DTOs.Categories;
using Application.DTOs.DiaryEntries;
using Application.DTOs.Objectives;
using Application.DTOs.Tasks;

namespace Application.DTOs.Users
{
    public class UserDataExportDto
    {
        public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
        public UserDto User { get; set; } = null!;
        public IEnumerable<CategoryDto> Categories { get; set; } = Array.Empty<CategoryDto>();
        public IEnumerable<ObjectiveDto> Objectives { get; set; } = Array.Empty<ObjectiveDto>();
        public IEnumerable<TaskDto> Tasks { get; set; } = Array.Empty<TaskDto>();
        public IEnumerable<DiaryEntryDto> DiaryEntries { get; set; } = Array.Empty<DiaryEntryDto>();
        public IEnumerable<BadgeDto> Badges { get; set; } = Array.Empty<BadgeDto>();
    }
}
