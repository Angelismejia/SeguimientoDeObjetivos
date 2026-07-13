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
        public DbSet<Follow> Follows { get; set; }
        public DbSet<FriendStreakInvitation> FriendStreakInvitations { get; set; }
        public DbSet<FriendStreak> FriendStreaks { get; set; }

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

            modelBuilder.Entity<Follow>(entity =>
            {
                entity.HasIndex(f => new { f.FollowerId, f.FollowingId }).IsUnique();

                entity.HasOne(f => f.Follower)
                    .WithMany()
                    .HasForeignKey(f => f.FollowerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(f => f.Following)
                    .WithMany()
                    .HasForeignKey(f => f.FollowingId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<FriendStreakInvitation>(entity =>
            {
                entity.Property(i => i.Status).HasMaxLength(20).IsRequired();

                entity.HasOne(i => i.FromUser)
                    .WithMany()
                    .HasForeignKey(i => i.FromUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(i => i.ToUser)
                    .WithMany()
                    .HasForeignKey(i => i.ToUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<FriendStreak>(entity =>
            {
                entity.HasIndex(f => new { f.UserAId, f.UserBId }).IsUnique();

                entity.HasOne(f => f.UserA)
                    .WithMany()
                    .HasForeignKey(f => f.UserAId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(f => f.UserB)
                    .WithMany()
                    .HasForeignKey(f => f.UserBId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }




    }
}