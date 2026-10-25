using Application.Common;
using System.Text.Json.Serialization;
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Messages
{
    public class MessageDto
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public string Content { get; set; } = string.Empty;
        [JsonConverter(typeof(InstanteUtcJsonConverter))]
        public DateTime SentAt { get; set; }
        [JsonConverter(typeof(InstanteUtcNullableJsonConverter))]
        public DateTime? ReadAt { get; set; }



    }
}
