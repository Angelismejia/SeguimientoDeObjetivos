using System.Security.Claims;
using Application.DTOs.Tasks;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly ITaskService _taskService;
        private readonly IObjectiveService _objectiveService;
        private readonly IFollowService _followService;

        public TasksController(ITaskService taskService, IObjectiveService objectiveService, IFollowService followService)
        {
            _taskService = taskService;
            _objectiveService = objectiveService;
            _followService = followService;
        }

        private int RequesterId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskDto>>> GetByUser([FromQuery] int userId)
        {
            var requesterId = RequesterId;
            if (requesterId != userId && !await _followService.IsFollowingAsync(requesterId, userId))
                return Forbid();

            return Ok(await _taskService.GetByUserIdAsync(userId));
        }

        [HttpGet("by-objective/{objectiveId}")]
        public async Task<ActionResult<IEnumerable<TaskDto>>> GetByObjective(int objectiveId)
        {
            var requesterId = RequesterId;
            var objective = await _objectiveService.GetByIdAsync(objectiveId);
            if (objective.UserId != requesterId && !await _followService.IsFollowingAsync(requesterId, objective.UserId))
                return Forbid();

            return Ok(await _taskService.GetByObjectiveIdAsync(objectiveId));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TaskDto>> GetById(int id)
        {
            var requesterId = RequesterId;
            var task = await _taskService.GetByIdAsync(id);
            if (task.UserId != requesterId && !await _followService.IsFollowingAsync(requesterId, task.UserId))
                return Forbid();

            return Ok(task);
        }

        [HttpPost]
        public async Task<ActionResult<TaskDto>> Create(CreateTaskDto dto)
        {
            var created = await _taskService.CreateAsync(RequesterId, dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<TaskDto>> Update(int id, UpdateTaskDto dto)
        {
            var existing = await _taskService.GetByIdAsync(id);
            if (existing.UserId != RequesterId) return Forbid();

            return Ok(await _taskService.UpdateAsync(id, dto));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _taskService.GetByIdAsync(id);
            if (existing.UserId != RequesterId) return Forbid();

            await _taskService.DeleteAsync(id);
            return NoContent();
        }
    }
}
