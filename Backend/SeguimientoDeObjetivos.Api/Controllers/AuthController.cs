using Application.DTOs.Users;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
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
    }
}
