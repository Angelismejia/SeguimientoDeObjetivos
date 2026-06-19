using System;
using Domain.Enums;

namespace Domain.Entities
{
    public class Objective
    {
        public int Id { get; set; } 
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public ObjectiveStatus Status { get; set; } = ObjectiveStatus.Pending;
        public int ProgressPercentage { get; set; } = 0;
        public int UserId { get; set; }
        public int? CategoryId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }


        public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
        public User User { get; set; } = null!;
        public Category? Category { get; set; }
    }
}

