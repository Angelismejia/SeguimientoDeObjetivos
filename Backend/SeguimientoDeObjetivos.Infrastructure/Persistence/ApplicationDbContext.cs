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


            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(u => u.Username).HasMaxLength(100).IsRequired();
                entity.Property(u => u.Name).HasMaxLength(100).IsRequired();
                entity.Property(u => u.Email).HasMaxLength(255).IsRequired();
                entity.Property(u => u.PasswordHash).IsRequired();
                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasIndex(u => u.Username).IsUnique();
            });

            modelBuilder.Entity<Objective>(entity =>
            {
                entity.Property(o => o.Title).HasMaxLength(150).IsRequired();
                entity.HasIndex(o => o.UserId);
            });

            modelBuilder.Entity<TaskItem>(entity =>
            {
                entity.Property(t => t.Title).HasMaxLength(150).IsRequired();
                entity.HasIndex(t => t.UserId);
                entity.HasIndex(t => t.ObjectiveId);
            });

            modelBuilder.Entity<DiaryEntry>(entity =>
            {
                entity.HasIndex(d => d.UserId);
            });

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasIndex(n => n.UserId);
            });

            modelBuilder.Entity<Category>(entity =>
            {
                entity.Property(c => c.Name).HasMaxLength(100).IsRequired();
                entity.HasIndex(c => c.UserId);
            });
        }




    }
}