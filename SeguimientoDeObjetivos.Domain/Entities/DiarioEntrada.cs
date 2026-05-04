namespace Domain.Entities
{
    public class DiarioEntrada
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Contenido { get; set; } = string.Empty;
        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        // Relación con User
        public int UserId { get; set; }
        public User User { get; set; } = null!;
    }
}