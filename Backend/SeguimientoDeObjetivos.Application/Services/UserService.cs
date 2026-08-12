using Application.DTOs.Users;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Exceptions;
using BCrypt.Net;

namespace Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IFollowRepository _followRepository;
        private readonly IMessageRepository _messageRepository;
        private readonly IFriendStreakInvitationRepository _friendStreakInvitationRepository;
        private readonly IFriendStreakRepository _friendStreakRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UserService(
            IUserRepository userRepository,
            IFollowRepository followRepository,
            IMessageRepository messageRepository,
            IFriendStreakInvitationRepository friendStreakInvitationRepository,
            IFriendStreakRepository friendStreakRepository,
            IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _followRepository = followRepository;
            _messageRepository = messageRepository;
            _friendStreakInvitationRepository = friendStreakInvitationRepository;
            _friendStreakRepository = friendStreakRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<UserDto>> GetAllAsync()
        {
            var users = await _userRepository.GetAllAsync();
            return users.Select(ToDto);
        }

        public async Task<UserDto> GetByIdAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user is null) throw new NotFoundException("User", id);
            return ToDto(user);
        }

        public async Task<UserDto> GetByUsernameAsync(string username)
        {
            var user = await _userRepository.GetByUsernameAsync(username);
            if (user is null) throw new NotFoundException("User", username);
            return ToDto(user);
        }

        public async Task<UserDto> CreateAsync(CreateUserDto dto)
        {
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            var user = new User
            {
                Username = dto.Username,
                Name = dto.Name,
                Email = dto.Email,
                PasswordHash = passwordHash,
                IsActive = true
            };

            var created = await _userRepository.CreateAsync(user);
            await _unitOfWork.SaveChangesAsync();
            return ToDto(created);
        }

        public async Task<UserDto> UpdateAsync(int id, UpdateUserDto dto)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user is null) throw new NotFoundException("User", id);

            user.Name = dto.Name;
            user.Email = dto.Email;
            user.IsActive = dto.IsActive;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();
            return ToDto(user);
        }

        public async Task<UserDto> UpdatePhotoAsync(int id, string photoUrl)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user is null) throw new NotFoundException("User", id);

            user.ProfilePhotoUrl = photoUrl;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();
            return ToDto(user);
        }

        public async Task ChangePasswordAsync(int id, ChangePasswordDto dto)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user is null) throw new NotFoundException("User", id);

            var passwordValid = BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash);
            if (!passwordValid)
                throw new InvalidOperationException("Current password is incorrect");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<UserDto> UpdateFollowPrivacyAsync(int id, UpdateFollowPrivacyDto dto)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user is null) throw new NotFoundException("User", id);

            user.AllowFollows = dto.AllowFollows;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();
            return ToDto(user);
        }

        public async Task DeleteAsync(int id)
        {
            var deleted = await _userRepository.DeleteAsync(id);
            if (!deleted) throw new NotFoundException("User", id);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteAccountAsync(int id, DeleteAccountDto dto)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user is null) throw new NotFoundException("User", id);

            var passwordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
            if (!passwordValid)
                throw new InvalidOperationException("La contraseña es incorrecta.");

            await _followRepository.DeleteAllForUserAsync(id);
            await _messageRepository.DeleteAllForUserAsync(id);
            await _friendStreakInvitationRepository.DeleteAllForUserAsync(id);
            await _friendStreakRepository.DeleteAllForUserAsync(id);
            await _userRepository.DeleteAsync(id);

            await _unitOfWork.SaveChangesAsync();
        }

        private static UserDto ToDto(User u) => new()
        {
            Id = u.Id,
            Username = u.Username,
            Name = u.Name,
            Email = u.Email,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt,
            UpdatedAt = u.UpdatedAt,
            ProfilePhotoUrl = u.ProfilePhotoUrl ?? string.Empty,
            AllowFollows = u.AllowFollows
        };
    }
}
