namespace Domain.Entities
{
    /// <summary>
    /// Un "me gusta" de un amigo a un objetivo logrado. Cada persona puede dar uno
    /// solo por objetivo (indice unico ObjectiveId + UserId).
    /// </summary>
    public class ObjectiveLike
    {
        public int Id { get; set; }
        public int ObjectiveId { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Objective Objective { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
