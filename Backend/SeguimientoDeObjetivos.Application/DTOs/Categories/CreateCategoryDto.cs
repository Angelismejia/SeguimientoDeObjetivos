using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Categories
{
    public class CreateCategoryDto
    {
        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(7)]
        public string? Color { get; set; }

        [MaxLength(50)]
        public string? Icon { get; set; }

        public bool IsDefault { get; set; } = false;
    }
}
