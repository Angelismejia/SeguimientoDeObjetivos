using Application.DTOs.Follows;

namespace Application.DTOs.Objectives
{
    /// <summary>Un objetivo del feed de Comunidad, con su dueño y sus reacciones.</summary>
    public class CommunityFeedItemDto
    {
        public ObjectiveDto Objective { get; set; } = null!;
        public UserSummaryDto Owner { get; set; } = null!;
        public ObjectiveSocialDto Social { get; set; } = null!;
    }
}
