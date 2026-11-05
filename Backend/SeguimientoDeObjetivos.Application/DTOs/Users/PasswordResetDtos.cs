using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Users
{
    public class ForgotPasswordDto
    {
        [Required]
        [EmailAddress]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordDto
    {
        [Required]
        [MaxLength(500)]
        public string Token { get; set; } = string.Empty;

        // Mismas reglas que al registrarse y que ChangePasswordDto.
        [Required]
        [MinLength(6)]
        [MaxLength(100)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
