using Application.DTOs.Objectives;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ObjectivesController : ControllerBase
    {
        private readonly IObjectiveService _objectiveService;

        public ObjectivesController(IObjectiveService objectiveService)
        {
            _objectiveService = objectiveService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ObjectiveDto>>> GetByUser([FromQuery] int userId)
        {
            var objectives = await _objectiveService.GetByUserIdAsync(userId);
            return Ok(objectives);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ObjectiveDto>> GetById(int id)
        {
            var objective = await _objectiveService.GetByIdAsync(id);
            if (objective is null) return NotFound();
            return Ok(objective);
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
            var updated = await _objectiveService.UpdateAsync(id, dto);
            if (updated is null) return NotFound();
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _objectiveService.DeleteAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }
    }
}
