using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.DiaryEntries
{
    public class UpdateDiaryEntryDto
    {
        [MaxLength(150)]
        public string? Title { get; set; }

        [Required]
        [MaxLength(5000)]
        public string Content { get; set; } = string.Empty;

        [Required]
        public DateTime EntryDate { get; set; }
    }
}
