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

        public ObjectivesController(IObjectiveService objectiveService)
        {
            _objectiveService = objectiveService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ObjectiveDto>>> GetByUser([FromQuery] int userId)
        {
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

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _objectiveService.DeleteAsync(id);
            return NoContent();
        }
    }
}
