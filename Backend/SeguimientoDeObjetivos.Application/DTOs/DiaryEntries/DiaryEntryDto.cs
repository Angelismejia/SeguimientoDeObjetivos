namespace Application.DTOs.DiaryEntries
{
    public class DiaryEntryDto
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime EntryDate { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
