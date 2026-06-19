using Domain.Enums;

namespace Application.DTOs.Objectives
{
    public class ObjectiveDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public ObjectiveStatus Status { get; set; }
        public int ProgressPercentage { get; set; }
        public int UserId { get; set; }
        public int? CategoryId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
