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

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ObjectiveDto>>> GetByUser([FromQuery] int userId)
        {
            var requesterId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            if (requesterId != userId && !await _followService.IsFollowingAsync(requesterId, userId))
                return Forbid();

            return Ok(await _objectiveService.GetByUserIdAsync(userId));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ObjectiveDto>> GetById(int id)
        {
            return Ok(await _objectiveService.GetByIdAsync(id));
        }

        [HttpPost]
        public async Task<ActionResult<ObjectiveDto>> Create([FromQuery] int userId, CreateObjectiveDto dto)
        {
            var created = await _objectiveService.CreateAsync(userId, dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ObjectiveDto>> Update(int id, UpdateObjectiveDto dto)
        {
            return Ok(await _objectiveService.UpdateAsync(id, dto));
        }

        [HttpPut("{id}/primary")]
        public async Task<ActionResult<ObjectiveDto>> SetPrimary(int id, [FromQuery] int userId, SetPrimaryObjectiveDto dto)
        {
            return Ok(await _objectiveService.SetPrimaryAsync(userId, id, dto.IsPrimary));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _objectiveService.DeleteAsync(id);
            return NoContent();
        }
    }
}
