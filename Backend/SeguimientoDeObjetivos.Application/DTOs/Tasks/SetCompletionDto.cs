namespace Application.DTOs.Tasks
{
    // Marca o desmarca una tarea recurrente en un dia concreto.
    public class SetCompletionDto
    {
        public DateTime Date { get; set; }
        public bool Completed { get; set; }
    }
}
