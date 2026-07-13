namespace Application.DTOs.FriendStreaks
{
    public class FriendStreakDto
    {
        public int Id { get; set; }
        public int PartnerId { get; set; }
        public string PartnerUsername { get; set; } = string.Empty;
        public string PartnerName { get; set; } = string.Empty;
        public string? PartnerProfilePhotoUrl { get; set; }
        public int CurrentStreak { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
