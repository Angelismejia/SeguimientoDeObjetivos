namespace Domain.Entities
{
    public class FriendStreakInvitation
    {
        public int Id { get; set; }
        public int FromUserId { get; set; }
        public int ToUserId { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User FromUser { get; set; } = null!;
        public User ToUser { get; set; } = null!;
    }
}
