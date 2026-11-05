namespace Application.DTOs.Objectives
{
    /// <summary>Likes y comentarios de un objetivo, vistos por quien los pide.</summary>
    public class ObjectiveSocialDto
    {
        public int ObjectiveId { get; set; }
        public int LikeCount { get; set; }
        public bool LikedByMe { get; set; }
        public int CommentCount { get; set; }
    }
}
