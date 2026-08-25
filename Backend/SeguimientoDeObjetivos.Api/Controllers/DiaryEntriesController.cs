using System.Security.Claims;
using Application.DTOs.DiaryEntries;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    // El diario es privado: a diferencia de tareas y objetivos, aca no hay
    // excepcion para seguidores. Solo su autor puede leerlo, editarlo o borrarlo.
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DiaryEntriesController : ControllerBase
    {
        private readonly IDiaryEntryService _diaryEntryService;

        public DiaryEntriesController(IDiaryEntryService diaryEntryService)
        {
            _diaryEntryService = diaryEntryService;
        }

        private int RequesterId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DiaryEntryDto>>> GetByUser([FromQuery] int userId)
        {
            if (userId != RequesterId) return Forbid();

            return Ok(await _diaryEntryService.GetByUserIdAsync(userId));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<DiaryEntryDto>> GetById(int id)
        {
            var entry = await _diaryEntryService.GetByIdAsync(id);
            if (entry.UserId != RequesterId) return Forbid();

            return Ok(entry);
        }

        // El userId sale del token y ya no del query: mandarlo por la URL permitia
        // crear entradas a nombre de otra persona.
        [HttpPost]
        public async Task<ActionResult<DiaryEntryDto>> Create(CreateDiaryEntryDto dto)
        {
            var created = await _diaryEntryService.CreateAsync(RequesterId, dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<DiaryEntryDto>> Update(int id, UpdateDiaryEntryDto dto)
        {
            var entry = await _diaryEntryService.GetByIdAsync(id);
            if (entry.UserId != RequesterId) return Forbid();

            return Ok(await _diaryEntryService.UpdateAsync(id, dto));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entry = await _diaryEntryService.GetByIdAsync(id);
            if (entry.UserId != RequesterId) return Forbid();

            await _diaryEntryService.DeleteAsync(id);
            return NoContent();
        }
    }
}
