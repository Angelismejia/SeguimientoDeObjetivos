namespace Domain.Entities
{
    public class Tarea
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public bool Completada { get; set; } = false;
        public DateTime? FechaVencimiento { get; set; }
        public DateTime CompletadaEn { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Relación con Objetivo
        public int ObjetivoId { get; set; }
        public Objetivo Objetivo { get; set; } = null!;
    }
}