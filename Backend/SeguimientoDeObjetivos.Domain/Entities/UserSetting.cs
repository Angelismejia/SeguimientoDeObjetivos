namespace Domain.Entities
{
    public class UserSetting
    {

        public int Id { get; set; }

        // FK = Users(Id)
        public int UserId { get; set; }
        public string Theme { get; set; } = "Light";
        public string Language { get; set; } = "es";
        public bool EmailNotificationsEnabled { get; set; } = true;
        public bool AppNotificationsEnabled { get; set; } = true;
        public TimeOnly? DailyReminderTime { get; set; }

        public User User { get; set; } = null!;


    }
}

