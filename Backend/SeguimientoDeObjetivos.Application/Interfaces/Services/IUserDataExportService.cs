using Application.DTOs.Users;

namespace Application.Interfaces.Services
{
    public interface IUserDataExportService
    {
        Task<UserDataExportDto> ExportAsync(int userId);
    }
}
