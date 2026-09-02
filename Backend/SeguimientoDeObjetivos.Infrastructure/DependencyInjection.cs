using Application.Interfaces;
using Application.Interfaces.Repositories;
using Infrastructure.BackgroundServices;
using Infrastructure.Common;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // La BD de produccion es Azure SQL serverless con auto-pause: si nadie
            // la usa un rato se apaga, y la primera consulta que llega mientras
            // esta despertando falla. No es un caso raro, es el arranque en frio
            // de todos los dias, y sin reintentos se le aparecia al usuario como
            // un error del formulario.
            //
            // Despertar tarda decenas de segundos, asi que el tope de espera es
            // alto a proposito. Es seguro porque no hay transacciones explicitas
            // en el codigo: con EnableRetryOnFailure, una transaccion abierta a
            // mano habria que reintentarla entera dentro de la estrategia.
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    sqlServer => sqlServer.EnableRetryOnFailure(
                        maxRetryCount: 6,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null)));

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddSingleton<IAppClock, AppClock>();

            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<IObjectiveRepository, ObjectiveRepository>();
            services.AddScoped<ITaskRepository, TaskRepository>();
            services.AddScoped<IDiaryEntryRepository, DiaryEntryRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IBadgeRepository, BadgeRepository>();
            services.AddScoped<IFollowRepository, FollowRepository>();
            services.AddScoped<IFriendStreakInvitationRepository, FriendStreakInvitationRepository>();
            services.AddScoped<IFriendStreakRepository, FriendStreakRepository>();
            services.AddScoped<IMessageRepository, MessageRepository>();
            services.AddScoped<IUserSettingRepository, UserSettingRepository>();
            services.AddScoped<ITaskCompletionRepository, TaskCompletionRepository>();

            services.AddHostedService<TaskReminderBackgroundService>();

            return services;
        }
    }
}
