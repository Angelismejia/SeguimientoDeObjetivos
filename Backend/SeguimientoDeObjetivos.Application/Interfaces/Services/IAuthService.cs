using Application.DTOs.Users;

namespace Application.Interfaces.Services
{
    public interface IAuthService
    {
        Task<LoginResponseDto> RegisterAsync(CreateUserDto createUserDto);
        Task<LoginResponseDto> LoginAsync(LoginRequestDto loginRequestDto);
    }
}
