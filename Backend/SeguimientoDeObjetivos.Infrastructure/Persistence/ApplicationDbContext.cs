using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

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
        public DbSet<TaskCompletion> TaskCompletions { get; set; }
        public DbSet<DiaryEntry> DiaryEntries { get; set; }
        public DbSet<Streak> Streaks { get; set; }
        public DbSet<Badge> Badges { get; set; }
        public DbSet<UserBadge> UserBadges { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Follow> Follows { get; set; }
        public DbSet<FriendStreakInvitation> FriendStreakInvitations { get; set; }
        public DbSet<FriendStreak> FriendStreaks { get; set; }

        /// <summary>
        /// Fuerza a que TODA fecha se guarde como "timestamp without time zone" y
        /// toda hora como "time without time zone".
        ///
        /// Sin esto la migracion a PostgreSQL no funciona. Npgsql mapea DateTime a
        /// "timestamptz" por defecto y **lanza excepcion si el Kind no es Utc**, y
        /// esta app mezcla los dos: DateTime.UtcNow da Kind=Utc, pero AppClock.Now
        /// (que convierte a la hora de Guatemala) da Kind=Unspecified, igual que
        /// cualquier fecha que llegue deserializada desde el JSON del front.
        ///
        /// "timestamp without time zone" se comporta como el datetime2 que usaba
        /// SQL Server: guarda el valor tal cual, sin interpretarlo ni convertirlo.
        /// Asi la semantica de fechas no cambia en absoluto al cambiar de motor,
        /// que es justo lo que se quiere en una migracion: mover los datos, no
        /// reinterpretarlos.
        ///
        /// El manejo de husos horarios sigue viviendo donde ya vivia, en AppClock.
        /// </summary>
        ///
        /// <remarks>
        /// Los conversores borran el Kind al escribir. Es lo que hacia SQL Server:
        /// datetime2 guarda los componentes de la fecha y descarta el Kind, asi que
        /// un DateTime.UtcNow terminaba guardado como "las 3 de la manana" sin decir
        /// de que huso. Reproducir esa conducta es lo correcto en una migracion: los
        /// valores que ya estan en la base fueron escritos con esa semantica.
        /// </remarks>
        private static readonly ValueConverter<DateTime, DateTime> SinZonaHoraria =
            new(alGuardar => DateTime.SpecifyKind(alGuardar, DateTimeKind.Unspecified),
                alLeer => DateTime.SpecifyKind(alLeer, DateTimeKind.Unspecified));

        private static readonly ValueConverter<DateTime?, DateTime?> SinZonaHorariaNullable =
            new(alGuardar => alGuardar.HasValue
                    ? DateTime.SpecifyKind(alGuardar.Value, DateTimeKind.Unspecified)
                    : alGuardar,
                alLeer => alLeer.HasValue
                    ? DateTime.SpecifyKind(alLeer.Value, DateTimeKind.Unspecified)
                    : alLeer);

        private static void AplicarTiposDeFechaDePostgres(ModelBuilder modelBuilder)
        {
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entity.GetProperties())
                {
                    // Respeta lo que ya se haya declarado a mano (TaskCompletion.Date
                    // es "date" a proposito: es un dia, no un instante).
                    if (property.GetColumnType() is not null) continue;

                    var tipo = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;

                    if (tipo == typeof(DateTime))
                    {
                        property.SetColumnType("timestamp without time zone");

                        // Elegir el tipo de columna no alcanza: Npgsql igual se niega a
                        // escribir un DateTime con Kind=Utc en una columna sin zona
                        // horaria, y las entidades nacen con DateTime.UtcNow. Sin este
                        // conversor, leer funciona pero CUALQUIER insercion falla.
                        property.SetValueConverter(property.ClrType == typeof(DateTime)
                            ? SinZonaHoraria
                            : SinZonaHorariaNullable);
                    }
                    else if (tipo == typeof(TimeSpan))
                        // Son horas del dia (ScheduledTime, EndTime), no duraciones:
                        // Npgsql las mapearia a "interval" y eso no es lo que son.
                        property.SetColumnType("time without time zone");
                }
            }
        }

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

            modelBuilder.Entity<TaskCompletion>(entity =>
            {
                // "date" existe igual en PostgreSQL, no hace falta cambiarlo.
                entity.Property(c => c.Date).HasColumnType("date");

                // Evita duplicados: una tarea no puede estar completada dos veces el mismo dia.
                entity.HasIndex(c => new { c.TaskId, c.Date }).IsUnique();

                entity.HasOne(c => c.Task)
                    .WithMany(t => t.Completions)
                    .HasForeignKey(c => c.TaskId)
                    .OnDelete(DeleteBehavior.Cascade);
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
            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasIndex(f => new { f.SenderId, f.ReceiverId });
                entity.HasOne(f => f.Sender)
                    .WithMany()
                    .HasForeignKey(f => f.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(f => f.Receiver)
                    .WithMany()
                    .HasForeignKey(f => f.ReceiverId)
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

            SembrarCatalogoDeInsignias(modelBuilder);

            // Va al final a proposito: asi ve todo lo que se configuro arriba y no
            // pisa los tipos elegidos a mano (TaskCompletion.Date es "date").
            AplicarTiposDeFechaDePostgres(modelBuilder);
        }

        /// <summary>
        /// El catalogo de insignias no son datos de usuario: son parte del esquema,
        /// y sin ellos BadgeAwardService no tiene nada que otorgar.
        ///
        /// Vivia dentro de la migracion SeedBadgeCatalog, no en el modelo. Eso
        /// funcionaba mientras nadie regenerara las migraciones — al hacerlo para
        /// PostgreSQL, el seed desaparecio y la tabla quedaba vacia sin que nada
        /// fallara. Con HasData el catalogo es parte del modelo y sobrevive a
        /// cualquier regeneracion futura.
        /// </summary>
        private static void SembrarCatalogoDeInsignias(ModelBuilder modelBuilder)
        {
            // Fecha fija: si fuera DateTime.UtcNow, EF detectaria un cambio en el
            // modelo cada vez que se genera una migracion.
            //
            // El Kind va Unspecified a proposito. CreatedAt es "timestamp without
            // time zone" (ver AplicarTiposDeFechaDePostgres) y Npgsql se niega a
            // escribir un DateTime marcado como Utc en una columna sin zona horaria:
            // "literal cannot be generated for a UTC DateTime". En SQL Server el Kind
            // daba igual y por eso la migracion vieja podia usar Utc sin problema.
            var sembradoEl = new DateTime(2026, 7, 24, 0, 0, 0, DateTimeKind.Unspecified);

            modelBuilder.Entity<Badge>().HasData(
                new Badge { Id = 1, Name = "Primer paso", Description = "Completaste tu primera tarea.", BadgeType = "first_task", Icon = "check_circle", CreatedAt = sembradoEl },
                new Badge { Id = 2, Name = "En marcha", Description = "Completaste 10 tareas.", BadgeType = "tasks_10", Icon = "task_alt", CreatedAt = sembradoEl },
                new Badge { Id = 3, Name = "Imparable", Description = "Completaste 50 tareas.", BadgeType = "tasks_50", Icon = "bolt", CreatedAt = sembradoEl },
                new Badge { Id = 4, Name = "Meta cumplida", Description = "Completaste tu primer objetivo.", BadgeType = "first_objective", Icon = "flag", CreatedAt = sembradoEl },
                new Badge { Id = 5, Name = "Coleccionista de metas", Description = "Completaste 5 objetivos.", BadgeType = "objectives_5", Icon = "military_tech", CreatedAt = sembradoEl },
                new Badge { Id = 6, Name = "Constancia", Description = "3 días seguidos completando tareas.", BadgeType = "streak_3", Icon = "local_fire_department", CreatedAt = sembradoEl },
                new Badge { Id = 7, Name = "Una semana fuerte", Description = "7 días seguidos completando tareas.", BadgeType = "streak_7", Icon = "whatshot", CreatedAt = sembradoEl },
                new Badge { Id = 8, Name = "Hábito de acero", Description = "30 días seguidos completando tareas.", BadgeType = "streak_30", Icon = "emoji_events", CreatedAt = sembradoEl }
            );
        }

    }
}