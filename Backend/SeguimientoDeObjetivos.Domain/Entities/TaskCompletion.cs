using System;

namespace Domain.Entities
{
    // Un registro por cada dia en que se completo una tarea recurrente.
    // Las tareas de un solo dia no usan esta tabla: les alcanza con su propio Status.
    public class TaskCompletion
    {
        public int Id { get; set; }
        public int TaskId { get; set; }

        // Solo la fecha del dia completado; la hora va en CompletedAt.
        public DateTime Date { get; set; }
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

        public TaskItem Task { get; set; } = null!;
    }
}
