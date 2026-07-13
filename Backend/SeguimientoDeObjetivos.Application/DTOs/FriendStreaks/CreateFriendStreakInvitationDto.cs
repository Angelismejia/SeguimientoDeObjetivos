using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.FriendStreaks
{
    public class CreateFriendStreakInvitationDto
    {
        [Required]
        public int ToUserId { get; set; }
    }
}
