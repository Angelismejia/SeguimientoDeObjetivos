using Application.DTOs.Users;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IPasswordResetService _passwordResetService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, IPasswordResetService passwordResetService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _passwordResetService = passwordResetService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<ActionResult<LoginResponseDto>> Register(CreateUserDto dto)
        {
            var response = await _authService.RegisterAsync(dto);
            return Ok(response);
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDto>> Login(LoginRequestDto dto)
        {
            var response = await _authService.LoginAsync(dto);
            return Ok(response);
        }

        // Siempre 204, exista o no el correo: asi no sirve para averiguar quien
        // tiene cuenta. El limite de 5 por minuto de este controller frena que
        // alguien lo use para llenarle la bandeja de correos a otro.
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto, CancellationToken ct)
        {
            try
            {
                await _passwordResetService.RequestAsync(dto.Email, ct);
            }
            catch (Exception ex)
            {
                // Si el SMTP falla, responder 500 solo para los correos que EXISTEN
                // delataria quien tiene cuenta. Se anota y se responde igual.
                _logger.LogError(ex, "No se pudo mandar el correo de recuperacion de contraseña");
            }
            return NoContent();
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            await _passwordResetService.ResetAsync(dto.Token, dto.NewPassword);
            return NoContent();
        }
    }
}
