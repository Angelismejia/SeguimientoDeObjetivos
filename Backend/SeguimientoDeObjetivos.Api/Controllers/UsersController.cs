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
        private readonly IAuthService _authService;
        private readonly IWebHostEnvironment _env;

        public UsersController(
            IUserService userService,
            IUserDataExportService dataExportService,
            IUserSettingService userSettingService,
            IAuthService authService,
            IWebHostEnvironment env)
        {
            _authService = authService;
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

        // Sin parametros devuelve todos, como siempre. Con query y/o limit busca en
        // el servidor (solo cuentas activas, ordenadas por nombre, maximo 100).
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAll([FromQuery] string? query = null, [FromQuery] int? limit = null)
        {
            var requesterId = RequesterId;
            var users = query is null && limit is null
                ? await _userService.GetAllAsync()
                : await _userService.SearchAsync(query, limit ?? 20);
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
            // Cambiar la clave cierra todas las sesiones (ver SelloDeSesion), esta
            // incluida. Se devuelve un token nuevo para que quien la cambio siga dentro.
            return Ok(new { accessToken = await _authService.IssueTokenAsync(id) });
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
        // Tope duro para el cuerpo entero: lo que pase de aca ni se termina de leer.
        // Es un poco mas que TamanoMaximo para que el caso normal reciba el mensaje
        // claro de abajo en vez de un 413 pelado.
        [RequestSizeLimit(FotosDePerfil.TamanoMaximo + 1024 * 1024)]
        public async Task<ActionResult<UserDto>> UploadPhoto(int id, IFormFile file)
        {
            if (id != RequesterId) return Forbid();

            if (file.Length == 0)
                return BadRequest("El archivo está vacío.");
            if (file.Length > FotosDePerfil.TamanoMaximo)
                return BadRequest("La foto no puede pesar más de 5 MB.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!FotosDePerfil.Extensiones.Contains(extension))
                return BadRequest("Formato no permitido. Usa JPG, PNG o WEBP.");

            var cabecera = new byte[12];
            int leidos;
            await using (var lectura = file.OpenReadStream())
            {
                leidos = await lectura.ReadAtLeastAsync(cabecera, cabecera.Length, throwOnEndOfStream: false);
            }
            if (!FotosDePerfil.CabeceraCoincide(cabecera.AsSpan(0, leidos), extension))
                return BadRequest("El archivo no es una imagen JPG, PNG o WEBP válida.");

            var uploadsFolder = PersistentStorage.UploadsPath(_env);
            var anterior = (await _userService.GetByIdAsync(id)).ProfilePhotoUrl;

            var fileName = $"user{id}-{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var actualizado = await _userService.UpdatePhotoAsync(id, $"/uploads/{fileName}");
            // Recien despues de guardar la nueva: si algo fallaba antes, el usuario
            // se quedaba sin ninguna foto.
            FotosDePerfil.Borrar(anterior, uploadsFolder);
            return Ok(actualizado);
        }

        // Habia tambien un DELETE {id} que borraba la cuenta SIN pedir la contraseña
        // (y sin limpiar seguidores ni mensajes). El front nunca lo uso, y dejaba
        // sin sentido el pedido de clave de este: con un token robado alcanzaba.
        [HttpDelete("{id}/account")]
        public async Task<IActionResult> DeleteAccount(int id, [FromBody] DeleteAccountDto dto)
        {
            if (id != RequesterId) return Forbid();
            var foto = (await _userService.GetByIdAsync(id)).ProfilePhotoUrl;
            await _userService.DeleteAccountAsync(id, dto);
            // La cuenta ya no existe: su foto tampoco tiene por que quedarse en el disco.
            FotosDePerfil.Borrar(foto, PersistentStorage.UploadsPath(_env));
            return NoContent();
        }
    }
}
