using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Users
{
    public class ChangePasswordDto
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        // El tope tiene que ser el mismo que el de LoginRequestDto.Password (100).
        // Sin el se podia fijar una clave mas larga de la que el login acepta
        // despues, y el usuario quedaba afuera de su propia cuenta.
        [Required]
        [MinLength(6)]
        [MaxLength(100)]
        public string NewPassword { get; set; } = string.Empty;

    }
}
