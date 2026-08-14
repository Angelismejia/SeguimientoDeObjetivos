using Domain.Entities;

namespace Application.Interfaces.Repositories
{
    public interface IUserSettingRepository
    {
        Task<UserSetting?> GetByUserIdAsync(int userId);
        Task<UserSetting> CreateAsync(UserSetting setting);
        Task<UserSetting> UpdateAsync(UserSetting setting);
    }
}
