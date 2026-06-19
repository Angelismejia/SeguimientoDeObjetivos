using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Services;
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
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            // repositorios
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<IObjectiveRepository, ObjectiveRepository>();
            services.AddScoped<ITaskRepository, TaskRepository>();
            services.AddScoped<IDiaryEntryRepository, DiaryEntryRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IBadgeRepository, BadgeRepository>();

            // servicios
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IObjectiveService, ObjectiveService>();
            services.AddScoped<ITaskService, TaskService>();
            services.AddScoped<IDiaryEntryService, DiaryEntryService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IBadgeService, BadgeService>();

            return services;
        }
    }
}
