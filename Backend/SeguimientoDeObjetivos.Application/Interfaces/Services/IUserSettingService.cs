using Application.DTOs.Users;

namespace Application.Interfaces.Services
{
    public interface IUserSettingService
    {
        Task<ThemeDto> GetThemeAsync(int userId);
        Task<ThemeDto> UpdateThemeAsync(int userId, ThemeDto dto);
    }
}
