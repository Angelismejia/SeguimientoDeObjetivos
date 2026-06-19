namespace Domain.Entities
{
    public class Badge
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string BadgeType { get; set; } = null!;
        public string? Icon { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


        public ICollection<UserBadge> UserBadges { get; set; } = new List<UserBadge>();
    }
}


