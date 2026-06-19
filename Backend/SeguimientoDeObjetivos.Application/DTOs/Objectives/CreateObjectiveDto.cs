using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Objectives
{
    public class CreateObjectiveDto
    {
        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? CategoryId { get; set; }
    }
}
