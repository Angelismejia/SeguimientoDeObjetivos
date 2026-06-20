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
            return Ok(await _diaryEntryService.GetByUserIdAsync(userId));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<DiaryEntryDto>> GetById(int id)
        {
            return Ok(await _diaryEntryService.GetByIdAsync(id));
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
            return Ok(await _diaryEntryService.UpdateAsync(id, dto));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _diaryEntryService.DeleteAsync(id);
            return NoContent();
        }
    }
}
