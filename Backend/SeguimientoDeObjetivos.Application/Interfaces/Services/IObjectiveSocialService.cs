using Application.DTOs.Objectives;

namespace Application.Interfaces.Services
{
    /// <summary>
    /// Likes y comentarios en objetivos logrados. Quien puede VER el objetivo lo
    /// decide el controller (igual que en GET api/objectives/{id}); este servicio
    /// pone las reglas de lo que se puede HACER con el.
    /// </summary>
    public interface IObjectiveSocialService
    {
        Task<IEnumerable<ObjectiveSocialDto>> GetSummariesAsync(int requesterId, IEnumerable<int> objectiveIds);
        Task<ObjectiveSocialDto> SetLikeAsync(int requesterId, int objectiveId, bool liked);
        Task<IEnumerable<ObjectiveCommentDto>> GetCommentsAsync(int objectiveId);
        Task<ObjectiveCommentDto> GetCommentAsync(int commentId);
        Task<ObjectiveCommentDto> AddCommentAsync(int requesterId, int objectiveId, CreateObjectiveCommentDto dto);
        Task DeleteCommentAsync(int commentId);
    }
}
