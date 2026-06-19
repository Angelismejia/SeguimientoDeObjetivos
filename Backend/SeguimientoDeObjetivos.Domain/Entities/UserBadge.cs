namespace Domain.Entities
{
    public class UserBadge
    {
        public int Id { get; set; }
        // FK = Users(Id)
        public int UserId { get; set; }
        // FK = Badges(Id)
        public int BadgeId { get; set; }
        public DateTime EarnedAt { get; set; } = DateTime.UtcNow;
        public User User { get; set; } = null!;
        public Badge Badge { get; set; } = null!;
    }
}
