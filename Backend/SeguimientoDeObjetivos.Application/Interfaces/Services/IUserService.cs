using Application.DTOs.Users;

namespace Application.Interfaces.Services
{
    public interface IUserService
    {
        Task<IEnumerable<UserDto>> GetAllAsync();
        Task<UserDto> GetByIdAsync(int id);
        Task<UserDto> GetByUsernameAsync(string username);
        Task<UserDto> CreateAsync(CreateUserDto dto);
        Task<UserDto> UpdateAsync(int id, UpdateUserDto dto);
        Task<UserDto> UpdatePhotoAsync(int id, string photoUrl);
        Task DeleteAsync(int id);
    }
}
