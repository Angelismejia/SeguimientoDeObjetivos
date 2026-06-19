namespace Domain.Entities
{
    public class TaskRepeatDay
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public int DayOfWeek { get; set; }

        public TaskItem TaskItem { get; set; }   = null!;
    }
}
