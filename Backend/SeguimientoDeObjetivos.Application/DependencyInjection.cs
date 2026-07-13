using Application.Interfaces.Services;
using Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IObjectiveService, ObjectiveService>();
            services.AddScoped<ITaskService, TaskService>();
            services.AddScoped<IDiaryEntryService, DiaryEntryService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IBadgeService, BadgeService>();
            services.AddScoped<IFollowService, FollowService>();
            services.AddScoped<IFriendStreakService, FriendStreakService>();

            return services;
        }
    }
}
