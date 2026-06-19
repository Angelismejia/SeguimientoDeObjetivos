namespace Application.DTOs.Badges
{
    public class BadgeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string BadgeType { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
