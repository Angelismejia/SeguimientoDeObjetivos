using System.Threading;

namespace Domain.Entities
{
    public class Objetivo
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Relación con User
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        // Relación con Tareas
        public ICollection<Tarea> Tareas { get; set; } = new List<Tarea>();

        // Progreso calculado
        public double ObtenerProgreso()
        {
            if (Tareas.Count == 0) return 0;
            return (Tareas.Count(t => t.Completada) / (double)Tareas.Count) * 100;
        }
    }
}