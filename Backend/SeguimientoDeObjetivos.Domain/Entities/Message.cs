namespace Domain.Entities
{
    public class Message
    {   

        public int Id { get; set; }
        public User Sender { get; set; } = null!;
        public User Receiver { get; set; } = null!;
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public string Content { get; set; } = null!;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; } 
    }
}


