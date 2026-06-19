using Application.DTOs.Tasks;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
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
            var tasks = await _taskService.GetByUserIdAsync(userId);
            return Ok(tasks);
        }

        [HttpGet("by-objective/{objectiveId}")]
        public async Task<ActionResult<IEnumerable<TaskDto>>> GetByObjective(int objectiveId)
        {
            var tasks = await _taskService.GetByObjectiveIdAsync(objectiveId);
            return Ok(tasks);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TaskDto>> GetById(int id)
        {
            var task = await _taskService.GetByIdAsync(id);
            if (task is null) return NotFound();
            return Ok(task);
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
            var updated = await _taskService.UpdateAsync(id, dto);
            if (updated is null) return NotFound();
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _taskService.DeleteAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }
    }
}
