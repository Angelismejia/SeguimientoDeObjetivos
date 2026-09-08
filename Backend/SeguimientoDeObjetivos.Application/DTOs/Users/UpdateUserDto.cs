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

        // IsActive NO va aca. Este PUT lo llama el propio usuario desde su perfil, y
        // AuthService.Login rechaza a los inactivos: mandar isActive:false era darse
        // de baja sin vuelta atras, porque no hay ninguna pantalla para reactivarse.
        // Para borrar la cuenta esta DELETE /api/users/{id}/account, que pide la clave.
    }
}
