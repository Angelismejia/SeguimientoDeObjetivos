using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Messages
{
    public class CreateMessageDto
    {
        [Required]
        public int ReceiverId { get; set; }

        // La columna es text (sin tope) en PostgreSQL, asi que el unico limite que
        // habia era el que el cliente quisiera respetar: un POST directo podia
        // guardar un mensaje de megabytes y despues romper el render del chat.
        [Required]
        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty;
    }
}
