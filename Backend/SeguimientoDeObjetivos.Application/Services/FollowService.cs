using Application.DTOs.Follows;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Exceptions;

namespace Application.Services
{
    public class FollowService : IFollowService
    {
        private readonly IFollowRepository _followRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;

        public FollowService(IFollowRepository followRepository, IUserRepository userRepository, IUnitOfWork unitOfWork)
        {
            _followRepository = followRepository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<UserSummaryDto>> GetFollowersAsync(int userId)
        {
            var follows = await _followRepository.GetFollowersAsync(userId);
            return follows.Select(f => ToSummary(f.Follower));
        }

        public async Task<IEnumerable<UserSummaryDto>> GetFollowingAsync(int userId)
        {
            var follows = await _followRepository.GetFollowingAsync(userId);
            return follows.Select(f => ToSummary(f.Following));
        }

        public async Task<FollowDto> CreateAsync(int followerId, CreateFollowDto dto)
        {
            if (followerId == dto.FollowingId)
                throw new InvalidOperationException("No puedes seguirte a ti mismo.");

            var targetUser = await _userRepository.GetByIdAsync(dto.FollowingId);
            if (targetUser is null) throw new NotFoundException("User", dto.FollowingId);

            var existing = await _followRepository.GetAsync(followerId, dto.FollowingId);
            if (existing is not null)
                throw new InvalidOperationException("Ya sigues a este usuario.");

            var follow = new Follow
            {
                FollowerId = followerId,
                FollowingId = dto.FollowingId
            };

            var created = await _followRepository.CreateAsync(follow);
            await _unitOfWork.SaveChangesAsync();

            return new FollowDto
            {
                Id = created.Id,
                FollowerId = created.FollowerId,
                FollowingId = created.FollowingId,
                CreatedAt = created.CreatedAt
            };
        }

        public async Task DeleteAsync(int followerId, int followingId)
        {
            var deleted = await _followRepository.DeleteAsync(followerId, followingId);
            if (!deleted) throw new NotFoundException("Follow", followingId);
            await _unitOfWork.SaveChangesAsync();
        }

        private static UserSummaryDto ToSummary(User u) => new()
        {
            Id = u.Id,
            Username = u.Username,
            Name = u.Name,
            ProfilePhotoUrl = u.ProfilePhotoUrl
        };
    }
}
