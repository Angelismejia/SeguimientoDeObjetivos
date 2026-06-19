using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<UserSetting> UserSettings { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Objective> Objectives { get; set; }
        public DbSet<TaskItem> Tasks { get; set; }
        public DbSet<TaskRepeatDay> TaskRepeatDays { get; set; }
        public DbSet<DiaryEntry> DiaryEntries { get; set; }
        public DbSet<Streak> Streaks { get; set; }
        public DbSet<Badge> Badges { get; set; }
        public DbSet<UserBadge> UserBadges { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Objective>()
                .Property(o => o.Status)
                .HasConversion<string>();

            modelBuilder.Entity<TaskItem>()
                .Property(t => t.Status)
                .HasConversion<string>();

            modelBuilder.Entity<TaskItem>()
                .Property(t => t.Priority)
                .HasConversion<string>();

            modelBuilder.Entity<TaskItem>()
                .Property(t => t.RecurrenceType)
                .HasConversion<string>();
        }
    }
}