using Application.DTOs.Users;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;

namespace Application.Services
{
    public class UserSettingService : IUserSettingService
    {
        private static readonly HashSet<string> ValidThemes = new(StringComparer.OrdinalIgnoreCase) { "Light", "Dark" };

        private readonly IUserSettingRepository _userSettingRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UserSettingService(IUserSettingRepository userSettingRepository, IUnitOfWork unitOfWork)
        {
            _userSettingRepository = userSettingRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<ThemeDto> GetThemeAsync(int userId)
        {
            var setting = await GetOrCreateAsync(userId);
            return new ThemeDto { Theme = setting.Theme };
        }

        public async Task<ThemeDto> UpdateThemeAsync(int userId, ThemeDto dto)
        {
            if (!ValidThemes.Contains(dto.Theme))
                throw new InvalidOperationException("Tema inválido. Usa 'Light' o 'Dark'.");

            var setting = await GetOrCreateAsync(userId);
            setting.Theme = dto.Theme;
            await _userSettingRepository.UpdateAsync(setting);
            await _unitOfWork.SaveChangesAsync();

            return new ThemeDto { Theme = setting.Theme };
        }

        private async Task<UserSetting> GetOrCreateAsync(int userId)
        {
            var setting = await _userSettingRepository.GetByUserIdAsync(userId);
            if (setting is not null) return setting;

            setting = new UserSetting { UserId = userId };
            await _userSettingRepository.CreateAsync(setting);
            await _unitOfWork.SaveChangesAsync();
            return setting;
        }
    }
}
