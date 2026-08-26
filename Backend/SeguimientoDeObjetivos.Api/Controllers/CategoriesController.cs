using System.Security.Claims;
using Application.DTOs.Categories;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        private int RequesterId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetByUser([FromQuery] int userId)
        {
            if (userId != RequesterId) return Forbid();

            return Ok(await _categoryService.GetByUserIdAsync(userId));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CategoryDto>> GetById(int id)
        {
            var category = await _categoryService.GetByIdAsync(id);
            if (category.UserId != RequesterId) return Forbid();

            return Ok(category);
        }

        // El dueño sale del token y ya no del query: mandarlo por la URL permitia
        // crear categorias a nombre de otra persona.
        [HttpPost]
        public async Task<ActionResult<CategoryDto>> Create(CreateCategoryDto dto)
        {
            var created = await _categoryService.CreateAsync(RequesterId, dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<CategoryDto>> Update(int id, UpdateCategoryDto dto)
        {
            var category = await _categoryService.GetByIdAsync(id);
            if (category.UserId != RequesterId) return Forbid();

            return Ok(await _categoryService.UpdateAsync(id, dto));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _categoryService.GetByIdAsync(id);
            if (category.UserId != RequesterId) return Forbid();

            await _categoryService.DeleteAsync(id);
            return NoContent();
        }
    }
}
