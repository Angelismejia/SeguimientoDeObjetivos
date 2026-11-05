namespace Domain.Entities
{
    /// <summary>
    /// Un comentario de animo en un objetivo logrado.
    /// </summary>
    public class ObjectiveComment
    {
        public int Id { get; set; }
        public int ObjectiveId { get; set; }
        public int UserId { get; set; }
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Objective Objective { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
