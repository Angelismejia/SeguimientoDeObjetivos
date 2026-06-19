using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Users
{
    public class UpdateUserDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }
}
