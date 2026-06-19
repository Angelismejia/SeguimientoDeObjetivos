using Application.DTOs.DiaryEntries;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DiaryEntriesController : ControllerBase
    {
        private readonly IDiaryEntryService _diaryEntryService;

        public DiaryEntriesController(IDiaryEntryService diaryEntryService)
        {
            _diaryEntryService = diaryEntryService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DiaryEntryDto>>> GetByUser([FromQuery] int userId)
        {
            var entries = await _diaryEntryService.GetByUserIdAsync(userId);
            return Ok(entries);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<DiaryEntryDto>> GetById(int id)
        {
            var entry = await _diaryEntryService.GetByIdAsync(id);
            if (entry is null) return NotFound();
            return Ok(entry);
        }

        [HttpPost]
        public async Task<ActionResult<DiaryEntryDto>> Create([FromQuery] int userId, CreateDiaryEntryDto dto)
        {
            var created = await _diaryEntryService.CreateAsync(userId, dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<DiaryEntryDto>> Update(int id, UpdateDiaryEntryDto dto)
        {
            var updated = await _diaryEntryService.UpdateAsync(id, dto);
            if (updated is null) return NotFound();
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _diaryEntryService.DeleteAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }
    }
}
