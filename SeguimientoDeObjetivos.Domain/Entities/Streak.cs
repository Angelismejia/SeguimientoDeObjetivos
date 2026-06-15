namespace Domain.Entities
{
    public class Streak
    {
        public int Id { get; set; }
        public int RachaActual { get; set; } = 0;
        public int RachaMaxima { get; set; } = 0;
        public DateTime UltimaActividad { get; set; }

        // Relación con User
        public int UserId { get; set; }
        public User User { get; set; } = null!;
    }
}