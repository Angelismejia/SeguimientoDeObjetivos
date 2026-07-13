using Application.DTOs.FriendStreaks;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;

namespace Application.Services
{
    public class FriendStreakService : IFriendStreakService
    {
        private readonly IFriendStreakInvitationRepository _invitationRepository;
        private readonly IFriendStreakRepository _friendStreakRepository;
        private readonly IFollowRepository _followRepository;
        private readonly IUserRepository _userRepository;
        private readonly ITaskRepository _taskRepository;
        private readonly IUnitOfWork _unitOfWork;

        public FriendStreakService(
            IFriendStreakInvitationRepository invitationRepository,
            IFriendStreakRepository friendStreakRepository,
            IFollowRepository followRepository,
            IUserRepository userRepository,
            ITaskRepository taskRepository,
            IUnitOfWork unitOfWork)
        {
            _invitationRepository = invitationRepository;
            _friendStreakRepository = friendStreakRepository;
            _followRepository = followRepository;
            _userRepository = userRepository;
            _taskRepository = taskRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<FriendStreakInvitationDto>> GetReceivedInvitationsAsync(int userId)
        {
            var invitations = await _invitationRepository.GetReceivedAsync(userId);
            return invitations.Select(ToDto);
        }

        public async Task<IEnumerable<FriendStreakInvitationDto>> GetSentInvitationsAsync(int userId)
        {
            var invitations = await _invitationRepository.GetSentAsync(userId);
            return invitations.Select(ToDto);
        }

        public async Task<FriendStreakInvitationDto> InviteAsync(int fromUserId, CreateFriendStreakInvitationDto dto)
        {
            if (fromUserId == dto.ToUserId)
                throw new InvalidOperationException("No puedes invitarte a vos mismo.");

            var targetUser = await _userRepository.GetByIdAsync(dto.ToUserId);
            if (targetUser is null) throw new NotFoundException("User", dto.ToUserId);

            var alreadyFollowing = await _followRepository.GetAsync(fromUserId, dto.ToUserId);
            if (alreadyFollowing is null)
                throw new InvalidOperationException("Solo puedes invitar a racha compartida a alguien que ya sigues.");

            var existingPair = await _friendStreakRepository.GetAsync(fromUserId, dto.ToUserId);
            if (existingPair is not null)
                throw new InvalidOperationException("Ya tienes una racha compartida con este usuario.");

            var existingInvitation = await _invitationRepository.GetPendingBetweenAsync(fromUserId, dto.ToUserId);
            if (existingInvitation is not null)
                throw new InvalidOperationException("Ya existe una invitación pendiente con este usuario.");

            var invitation = new FriendStreakInvitation
            {
                FromUserId = fromUserId,
                ToUserId = dto.ToUserId
            };

            var created = await _invitationRepository.CreateAsync(invitation);
            await _unitOfWork.SaveChangesAsync();

            var fromUser = await _userRepository.GetByIdAsync(fromUserId);
            created.FromUser = fromUser!;
            return ToDto(created);
        }

        public async Task<FriendStreakDto> AcceptAsync(int invitationId, int currentUserId)
        {
            var invitation = await _invitationRepository.GetByIdAsync(invitationId);
            if (invitation is null) throw new NotFoundException("FriendStreakInvitation", invitationId);
            if (invitation.ToUserId != currentUserId)
                throw new InvalidOperationException("Esta invitación no es tuya.");
            if (invitation.Status != "Pending")
                throw new InvalidOperationException("Esta invitación ya fue respondida.");

            invitation.Status = "Accepted";
            await _invitationRepository.UpdateAsync(invitation);

            var friendStreak = new FriendStreak
            {
                UserAId = invitation.FromUserId,
                UserBId = invitation.ToUserId
            };
            var created = await _friendStreakRepository.CreateAsync(friendStreak);
            await _unitOfWork.SaveChangesAsync();

            return await ToFriendStreakDtoAsync(created, currentUserId);
        }

        public async Task RejectAsync(int invitationId, int currentUserId)
        {
            var invitation = await _invitationRepository.GetByIdAsync(invitationId);
            if (invitation is null) throw new NotFoundException("FriendStreakInvitation", invitationId);
            if (invitation.ToUserId != currentUserId)
                throw new InvalidOperationException("Esta invitación no es tuya.");
            if (invitation.Status != "Pending")
                throw new InvalidOperationException("Esta invitación ya fue respondida.");

            invitation.Status = "Rejected";
            await _invitationRepository.UpdateAsync(invitation);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<IEnumerable<FriendStreakDto>> GetForUserAsync(int userId)
        {
            var friendStreaks = await _friendStreakRepository.GetForUserAsync(userId);
            var result = new List<FriendStreakDto>();
            foreach (var fs in friendStreaks)
            {
                result.Add(await ToFriendStreakDtoAsync(fs, userId));
            }
            return result;
        }

        private async Task<FriendStreakDto> ToFriendStreakDtoAsync(FriendStreak fs, int currentUserId)
        {
            var partnerId = fs.UserAId == currentUserId ? fs.UserBId : fs.UserAId;
            var partner = (fs.UserAId == currentUserId ? fs.UserB : fs.UserA)
                ?? await _userRepository.GetByIdAsync(partnerId);

            var streak = await ComputeSharedStreakAsync(currentUserId, partnerId);

            return new FriendStreakDto
            {
                Id = fs.Id,
                PartnerId = partnerId,
                PartnerUsername = partner?.Username ?? string.Empty,
                PartnerName = partner?.Name ?? string.Empty,
                PartnerProfilePhotoUrl = partner?.ProfilePhotoUrl,
                CurrentStreak = streak,
                CreatedAt = fs.CreatedAt
            };
        }

        private async Task<int> ComputeSharedStreakAsync(int userAId, int userBId)
        {
            var tasksA = await _taskRepository.GetByUserIdAsync(userAId);
            var tasksB = await _taskRepository.GetByUserIdAsync(userBId);

            var daysA = CompletedDays(tasksA);
            var daysB = CompletedDays(tasksB);

            var today = DateTime.Today;
            var cursor = today;
            if (!(daysA.Contains(today) && daysB.Contains(today)))
            {
                cursor = today.AddDays(-1);
            }

            var streak = 0;
            while (daysA.Contains(cursor) && daysB.Contains(cursor))
            {
                streak++;
                cursor = cursor.AddDays(-1);
            }
            return streak;
        }

        private static HashSet<DateTime> CompletedDays(IEnumerable<TaskItem> tasks) =>
            tasks
                .Where(t => t.Status == TaskItemStatus.Completed)
                .Select(t => t.ScheduledDate.Date)
                .ToHashSet();

        private static FriendStreakInvitationDto ToDto(FriendStreakInvitation i) => new()
        {
            Id = i.Id,
            FromUserId = i.FromUserId,
            FromUsername = i.FromUser?.Username ?? string.Empty,
            FromName = i.FromUser?.Name ?? string.Empty,
            FromProfilePhotoUrl = i.FromUser?.ProfilePhotoUrl,
            ToUserId = i.ToUserId,
            Status = i.Status,
            CreatedAt = i.CreatedAt
        };
    }
}
