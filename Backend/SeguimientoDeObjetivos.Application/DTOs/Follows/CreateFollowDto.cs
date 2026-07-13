using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Follows
{
    public class CreateFollowDto
    {
        [Required]
        public int FollowingId { get; set; }
    }
}
