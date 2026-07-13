namespace Application.DTOs.FriendStreaks
{
    public class FriendStreakInvitationDto
    {
        public int Id { get; set; }
        public int FromUserId { get; set; }
        public string FromUsername { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
        public string? FromProfilePhotoUrl { get; set; }
        public int ToUserId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
