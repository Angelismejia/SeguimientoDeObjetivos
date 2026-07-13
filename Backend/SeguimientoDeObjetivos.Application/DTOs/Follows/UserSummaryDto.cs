namespace Application.DTOs.Follows
{
    public class UserSummaryDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? ProfilePhotoUrl { get; set; }
    }
}
