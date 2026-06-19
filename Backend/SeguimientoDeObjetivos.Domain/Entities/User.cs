namespace Domain.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string KeycloakUserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public UserSetting? UserSetting { get; set; }

        public ICollection<Category> Categories { get; set; } = new List<Category>();

        public ICollection<Objective> Objectives { get; set; } = new List<Objective>();

        public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();

        public ICollection<DiaryEntry> DiaryEntries { get; set; } = new List<DiaryEntry>();

        public Streak? Streak { get; set; }

        public ICollection<UserBadge> UserBadges { get; set; } = new List<UserBadge>();

        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    }
}

