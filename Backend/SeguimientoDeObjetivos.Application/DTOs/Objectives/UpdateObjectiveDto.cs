using Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Objectives
{
    public class UpdateObjectiveDto
    {
        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public ObjectiveStatus Status { get; set; }

        [Range(0, 100)]
        public int ProgressPercentage { get; set; }

        public int? CategoryId { get; set; }
    }
}
