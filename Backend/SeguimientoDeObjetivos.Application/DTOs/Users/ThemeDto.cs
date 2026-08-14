using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Users
{
    public class ThemeDto
    {
        [Required]
        public string Theme { get; set; } = "Light";
    }
}
