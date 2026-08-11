using System.Security.Claims;
using Application.DTOs.Users;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IUserDataExportService _dataExportService;
        private readonly IUserSettingService _userSettingService;
        private readonly IWebHostEnvironment _env;

        public UsersController(
            IUserService userService,
            IUserDataExportService dataExportService,
            IUserSettingService userSettingService,
            IWebHostEnvironment env)
        {
            _userService = userService;
            _dataExportService = dataExportService;
            _userSettingService = userSettingService;
            _env = env;
        }

        private int RequesterId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // El email es dato privado: solo se devuelve completo cuando el que pregunta
        // es el dueño del perfil. Para perfiles de otros usuarios (busqueda, seguidores,
        // chat) se oculta, igual que ya se hace con los objetivos privados.
        private static void RedactEmailIfNotSelf(UserDto dto, int requesterId)
        {
            if (dto.Id != requesterId) dto.Email = string.Empty;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAll()
        {
            var requesterId = RequesterId;
            var users = await _userService.GetAllAsync();
            foreach (var u in users) RedactEmailIfNotSelf(u, requesterId);
            return Ok(users);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<UserDto>> GetById(int id)
        {
            var user = await _userService.GetByIdAsync(id);
            RedactEmailIfNotSelf(user, RequesterId);
            return Ok(user);
        }

        [HttpGet("by-username/{username}")]
        public async Task<ActionResult<UserDto>> GetByUsername(string username)
        {
            var user = await _userService.GetByUsernameAsync(username);
            RedactEmailIfNotSelf(user, RequesterId);
            return Ok(user);
        }

        [HttpPost]
        public async Task<ActionResult<UserDto>> Create(CreateUserDto dto)
        {
            var created = await _userService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<UserDto>> Update(int id, UpdateUserDto dto)
        {
            if (id != RequesterId) return Forbid();
            return Ok(await _userService.UpdateAsync(id, dto));
        }

        [HttpPut("{id}/password")]
        public async Task<IActionResult> ChangePassword(int id, ChangePasswordDto dto)
        {
            if (id != RequesterId) return Forbid();
            await _userService.ChangePasswordAsync(id, dto);
            return NoContent();
        }

        [HttpPut("{id}/allow-follows")]
        public async Task<ActionResult<UserDto>> UpdateFollowPrivacy(int id, UpdateFollowPrivacyDto dto)
        {
            if (id != RequesterId) return Forbid();
            return Ok(await _userService.UpdateFollowPrivacyAsync(id, dto));
        }

        [HttpGet("{id}/theme")]
        public async Task<ActionResult<ThemeDto>> GetTheme(int id)
        {
            if (id != RequesterId) return Forbid();
            return Ok(await _userSettingService.GetThemeAsync(id));
        }

        [HttpPut("{id}/theme")]
        public async Task<ActionResult<ThemeDto>> UpdateTheme(int id, ThemeDto dto)
        {
            if (id != RequesterId) return Forbid();
            return Ok(await _userSettingService.UpdateThemeAsync(id, dto));
        }

        [HttpGet("{id}/export")]
        public async Task<ActionResult<UserDataExportDto>> ExportData(int id)
        {
            if (id != RequesterId) return Forbid();
            return Ok(await _dataExportService.ExportAsync(id));
        }

        [HttpPost("{id}/photo")]
        public async Task<ActionResult<UserDto>> UploadPhoto(int id, IFormFile file)
        {
            if (id != RequesterId) return Forbid();

            if (file.Length == 0)
                return BadRequest("El archivo está vacío.");

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
                return BadRequest("Formato no permitido. Usa JPG, PNG o WEBP.");

            var uploadsFolder = PersistentStorage.UploadsPath(_env);

            var fileName = $"user{id}-{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var photoUrl = $"/uploads/{fileName}";
            return Ok(await _userService.UpdatePhotoAsync(id, photoUrl));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (id != RequesterId) return Forbid();
            await _userService.DeleteAsync(id);
            return NoContent();
        }

        [HttpDelete("{id}/account")]
        public async Task<IActionResult> DeleteAccount(int id, [FromBody] DeleteAccountDto dto)
        {
            if (id != RequesterId) return Forbid();
            await _userService.DeleteAccountAsync(id, dto);
            return NoContent();
        }
    }
}
