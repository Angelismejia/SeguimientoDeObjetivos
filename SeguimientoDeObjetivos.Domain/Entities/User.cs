namespace Domain.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Relaciones
        public ICollection<Objetivo> Objetivos { get; set; } = new List<Objetivo>();
        public ICollection<DiarioEntrada> Entradas { get; set; } = new List<DiarioEntrada>();
    }
}