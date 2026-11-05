using Domain.Entities;

namespace Application.Interfaces.Repositories
{
    public interface IObjectiveSocialRepository
    {
        Task<ObjectiveLike?> GetLikeAsync(int objectiveId, int userId);
        Task<IEnumerable<ObjectiveLike>> GetLikesAsync(IEnumerable<int> objectiveIds);
        void AddLike(ObjectiveLike like);
        void RemoveLike(ObjectiveLike like);

        Task<IEnumerable<ObjectiveComment>> GetCommentsAsync(int objectiveId);
        Task<Dictionary<int, int>> CountCommentsAsync(IEnumerable<int> objectiveIds);
        Task<ObjectiveComment?> GetCommentAsync(int id);
        void AddComment(ObjectiveComment comment);
        void RemoveComment(ObjectiveComment comment);
    }
}
