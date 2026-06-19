namespace Domain.Entities
{
    public class Streak
    {

        public int Id { get; set; }
        public int UserId { get; set; }
        public int CurrentStreak { get; set; } = 0;
        public int BestStreak { get; set; } = 0;
        public DateTime? LastCompletedDate { get; set; }
        public DateTime? UpdatedAt { get; set; }



        public User User { get; set; } = null!;
    }
}
