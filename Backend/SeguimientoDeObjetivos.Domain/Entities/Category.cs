namespace Domain.Entities
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Color { get; set; }
        public string? Icon { get; set; }
        public bool IsDefault { get; set; } = false;
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; } = null!;

        public ICollection<Objective> Objectives { get; set; } = new List<Objective>();
        public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    }
}


