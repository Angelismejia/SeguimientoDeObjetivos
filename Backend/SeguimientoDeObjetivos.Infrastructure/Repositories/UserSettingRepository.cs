using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class UserSettingRepository : IUserSettingRepository
    {
        private readonly ApplicationDbContext _context;

        public UserSettingRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<UserSetting?> GetByUserIdAsync(int userId)
            => await _context.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId);

        public Task<UserSetting> CreateAsync(UserSetting setting)
        {
            _context.UserSettings.Add(setting);
            return Task.FromResult(setting);
        }

        public Task<UserSetting> UpdateAsync(UserSetting setting)
        {
            _context.UserSettings.Update(setting);
            return Task.FromResult(setting);
        }
    }
}
