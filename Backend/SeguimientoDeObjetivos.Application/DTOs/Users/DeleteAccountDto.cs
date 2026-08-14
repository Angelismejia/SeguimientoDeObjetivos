using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Users
{
    public class DeleteAccountDto
    {
        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
