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

        [Required]
        public string Content { get; set; } = string.Empty;
    }
}
