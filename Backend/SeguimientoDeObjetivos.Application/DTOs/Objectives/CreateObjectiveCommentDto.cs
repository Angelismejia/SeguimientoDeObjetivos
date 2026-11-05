using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Objectives
{
    public class CreateObjectiveCommentDto
    {
        [Required(ErrorMessage = "El comentario no puede estar vacío.")]
        [MaxLength(500, ErrorMessage = "El comentario no puede pasar de 500 caracteres.")]
        public string Content { get; set; } = string.Empty;
    }
}
