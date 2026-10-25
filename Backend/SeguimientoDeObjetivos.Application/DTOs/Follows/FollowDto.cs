using Application.Common;
using System.Text.Json.Serialization;
namespace Application.DTOs.Follows
{
    public class FollowDto
    {
        public int Id { get; set; }
        public int FollowerId { get; set; }
        public int FollowingId { get; set; }
        [JsonConverter(typeof(InstanteUtcJsonConverter))]
        public DateTime CreatedAt { get; set; }
    }
}
