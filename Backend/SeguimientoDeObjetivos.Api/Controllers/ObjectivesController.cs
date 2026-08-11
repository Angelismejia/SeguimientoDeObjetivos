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

        public ObjectivesController(IObjectiveService objectiveService, IFollowService followService)
        {
            _objectiveService = objectiveService;
            _followService = followService;
        }

        private int RequesterId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

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
            var requesterId = RequesterId;
            var objective = await _objectiveService.GetByIdAsync(id);
            if (objective.UserId != requesterId)
            {
                if (objective.IsPrivate || !await _followService.IsFollowingAsync(requesterId, objective.UserId))
                    return Forbid();
            }

            return Ok(objective);
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
