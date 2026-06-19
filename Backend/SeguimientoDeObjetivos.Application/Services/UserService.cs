using Application.DTOs.Users;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;

namespace Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UserService(IUserRepository userRepository, IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<UserDto>> GetAllAsync()
        {
            var users = await _userRepository.GetAllAsync();
            return users.Select(ToDto);
        }

        public async Task<UserDto?> GetByIdAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            return user is null ? null : ToDto(user);
        }

        public async Task<UserDto> CreateAsync(CreateUserDto dto)
        {
            var user = new User
            {
                KeycloakUserId = dto.KeycloakUserId,
                Name = dto.Name,
                Email = dto.Email
            };

            var created = await _userRepository.CreateAsync(user);
            await _unitOfWork.SaveChangesAsync();
            return ToDto(created);
        }

        public async Task<UserDto?> UpdateAsync(int id, UpdateUserDto dto)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user is null) return null;

            user.Name = dto.Name;
            user.Email = dto.Email;
            user.IsActive = dto.IsActive;
            user.UpdatedAt = DateTime.UtcNow;

            var updated = await _userRepository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();
            return ToDto(updated);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var deleted = await _userRepository.DeleteAsync(id);
            if (!deleted) return false;
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        private static UserDto ToDto(User u) => new()
        {
            Id = u.Id,
            KeycloakUserId = u.KeycloakUserId,
            Name = u.Name,
            Email = u.Email,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt,
            UpdatedAt = u.UpdatedAt
        };
    }
}
