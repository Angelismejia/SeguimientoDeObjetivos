using System.Linq;
using System.Security.Claims;
using Application.DTOs.Objectives;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ObjectivesController : ControllerBase
    {
        private readonly IObjectiveService _objectiveService;
        private readonly IFollowService _followService;
        private readonly IObjectiveSocialService _socialService;

        public ObjectivesController(
            IObjectiveService objectiveService,
            IFollowService followService,
            IObjectiveSocialService socialService)
        {
            _objectiveService = objectiveService;
            _followService = followService;
            _socialService = socialService;
        }

        private int RequesterId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // La misma regla que GetById: lo tuyo siempre; lo de otro, solo si lo sigues
        // y el objetivo no es privado. Likes y comentarios pasan por aca, asi que no
        // se puede reaccionar a algo que no podrias ver.
        private async Task<bool> PuedeVerAsync(int requesterId, ObjectiveDto objective)
            => objective.UserId == requesterId
            || (!objective.IsPrivate && await _followService.IsFollowingAsync(requesterId, objective.UserId));

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ObjectiveDto>>> GetByUser([FromQuery] int userId)
        {
            var requesterId = RequesterId;
            if (requesterId != userId && !await _followService.IsFollowingAsync(requesterId, userId))
                return Forbid();

            var objectives = await _objectiveService.GetByUserIdAsync(userId);
            if (requesterId != userId)
                objectives = objectives.Where(o => !o.IsPrivate);

            return Ok(objectives);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ObjectiveDto>> GetById(int id)
        {
            var objective = await _objectiveService.GetByIdAsync(id);
            if (!await PuedeVerAsync(RequesterId, objective)) return Forbid();

            return Ok(objective);
        }

        // ── Comunidad: likes y comentarios ─────────────────────

        // El feed de Comunidad: objetivos publicos de quienes sigues, con su dueño
        // y sus reacciones, en una sola peticion. Solo muestra lo del propio
        // usuario que pide, asi que no necesita mas control que el token.
        [HttpGet("feed")]
        public async Task<ActionResult<IEnumerable<CommunityFeedItemDto>>> GetFeed([FromQuery] int limit = 30)
            => Ok(await _socialService.GetFeedAsync(RequesterId, limit));


        [HttpPut("{id}/like")]
        public async Task<ActionResult<ObjectiveSocialDto>> Like(int id)
            => await CambiarLike(id, true);

        [HttpDelete("{id}/like")]
        public async Task<ActionResult<ObjectiveSocialDto>> Unlike(int id)
            => await CambiarLike(id, false);

        private async Task<ActionResult<ObjectiveSocialDto>> CambiarLike(int id, bool liked)
        {
            var requesterId = RequesterId;
            var objective = await _objectiveService.GetByIdAsync(id);
            if (!await PuedeVerAsync(requesterId, objective)) return Forbid();

            return Ok(await _socialService.SetLikeAsync(requesterId, id, liked));
        }

        [HttpGet("{id}/comments")]
        public async Task<ActionResult<IEnumerable<ObjectiveCommentDto>>> GetComments(int id)
        {
            var objective = await _objectiveService.GetByIdAsync(id);
            if (!await PuedeVerAsync(RequesterId, objective)) return Forbid();

            return Ok(await _socialService.GetCommentsAsync(id));
        }

        [HttpPost("{id}/comments")]
        public async Task<ActionResult<ObjectiveCommentDto>> AddComment(int id, CreateObjectiveCommentDto dto)
        {
            var requesterId = RequesterId;
            var objective = await _objectiveService.GetByIdAsync(id);
            if (!await PuedeVerAsync(requesterId, objective)) return Forbid();

            return Ok(await _socialService.AddCommentAsync(requesterId, id, dto));
        }

        // Un comentario lo puede borrar quien lo escribio o el dueño del objetivo
        // (es su logro: puede limpiar lo que no quiere que se vea ahi).
        [HttpDelete("comments/{commentId}")]
        public async Task<IActionResult> DeleteComment(int commentId)
        {
            var requesterId = RequesterId;
            var comment = await _socialService.GetCommentAsync(commentId);
            if (comment.UserId != requesterId)
            {
                var objective = await _objectiveService.GetByIdAsync(comment.ObjectiveId);
                if (objective.UserId != requesterId) return Forbid();
            }

            await _socialService.DeleteCommentAsync(commentId);
            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<ObjectiveDto>> Create(CreateObjectiveDto dto)
        {
            var created = await _objectiveService.CreateAsync(RequesterId, dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ObjectiveDto>> Update(int id, UpdateObjectiveDto dto)
        {
            var existing = await _objectiveService.GetByIdAsync(id);
            if (existing.UserId != RequesterId) return Forbid();

            return Ok(await _objectiveService.UpdateAsync(id, dto));
        }

        [HttpPut("{id}/primary")]
        public async Task<ActionResult<ObjectiveDto>> SetPrimary(int id, SetPrimaryObjectiveDto dto)
        {
            var requesterId = RequesterId;
            var existing = await _objectiveService.GetByIdAsync(id);
            if (existing.UserId != requesterId) return Forbid();

            return Ok(await _objectiveService.SetPrimaryAsync(requesterId, id, dto.IsPrimary));
        }

        [HttpPut("{id}/privacy")]
        public async Task<ActionResult<ObjectiveDto>> SetPrivate(int id, SetPrivateObjectiveDto dto)
        {
            var requesterId = RequesterId;
            var existing = await _objectiveService.GetByIdAsync(id);
            if (existing.UserId != requesterId) return Forbid();

            return Ok(await _objectiveService.SetPrivateAsync(requesterId, id, dto.IsPrivate));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _objectiveService.GetByIdAsync(id);
            if (existing.UserId != RequesterId) return Forbid();

            await _objectiveService.DeleteAsync(id);
            return NoContent();
        }
    }
}
