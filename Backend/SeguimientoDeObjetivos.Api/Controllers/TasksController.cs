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

        public TasksController(ITaskService taskService)
        {
            _taskService = taskService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskDto>>> GetByUser([FromQuery] int userId)
        {
            return Ok(await _taskService.GetByUserIdAsync(userId));
        }

        [HttpGet("by-objective/{objectiveId}")]
        public async Task<ActionResult<IEnumerable<TaskDto>>> GetByObjective(int objectiveId)
        {
            return Ok(await _taskService.GetByObjectiveIdAsync(objectiveId));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TaskDto>> GetById(int id)
        {
            return Ok(await _taskService.GetByIdAsync(id));
        }

        [HttpPost]
        public async Task<ActionResult<TaskDto>> Create([FromQuery] int userId, CreateTaskDto dto)
        {
            var created = await _taskService.CreateAsync(userId, dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<TaskDto>> Update(int id, UpdateTaskDto dto)
        {
            return Ok(await _taskService.UpdateAsync(id, dto));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _taskService.DeleteAsync(id);
            return NoContent();
        }
    }
}
