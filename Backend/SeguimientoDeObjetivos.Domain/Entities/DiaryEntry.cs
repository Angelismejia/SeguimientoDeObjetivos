namespace Domain.Entities
{
    public class DiaryEntry
    {
        public int Id { get; set; }
        public string? Title { get; set; } 
        public string Content { get; set; } = null!;
        public DateTime EntryDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public int UserId { get; set; }
        
        public User User { get; set; } = null!;
    }
}