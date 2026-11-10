using Application.Common;
using Application.DTOs.Users;
using Domain.Exceptions;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Application.Services
{
    public class AuthService : IAuthService
    {
        private static readonly (string Name, string Color, string Icon)[] DefaultCategories =
        {
            ("Trabajo", "#4f46e5", "work"),
            ("Salud", "#16a34a", "favorite"),
            ("Estudio", "#7c3aed", "school"),
            ("Personal", "#ea580c", "home"),
            ("Finanzas", "#0891b2", "savings")
        };

        private readonly IUserRepository _userRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly string _jwtSecret;
        private readonly string _jwtIssuer;
        private readonly string _jwtAudience;
        private readonly int _jwtExpirationMinutes;

        public AuthService(IUserRepository userRepository, ICategoryRepository categoryRepository, IUnitOfWork unitOfWork, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _categoryRepository = categoryRepository;
            _unitOfWork = unitOfWork;
            _jwtSecret = configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
            _jwtIssuer = configuration["Jwt:Issuer"] ?? "SeguimientoObjetivos";
            _jwtAudience = configuration["Jwt:Audience"] ?? "SeguimientoObjetivosUsers";
            _jwtExpirationMinutes = int.Parse(configuration["Jwt:ExpirationMinutes"] ?? "1440");
        }

        public async Task<LoginResponseDto> RegisterAsync(CreateUserDto createUserDto)
        {
            var existingUser = await _userRepository.GetByUsernameAsync(createUserDto.Username);
            if (existingUser != null)
                throw new BusinessRuleException("Ese nombre de usuario ya está en uso.");

            var existingEmail = await _userRepository.GetByEmailAsync(createUserDto.Email);
            if (existingEmail != null)
                throw new BusinessRuleException("Ya hay una cuenta con ese correo.");

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(createUserDto.Password);

            var user = new User
            {
                Username = createUserDto.Username,
                Name = createUserDto.Name,
                Email = createUserDto.Email,
                PasswordHash = passwordHash,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _userRepository.Add(user);
            await _unitOfWork.SaveChangesAsync();

            foreach (var (name, color, icon) in DefaultCategories)
            {
                await _categoryRepository.CreateAsync(new Category
                {
                    Name = name,
                    Color = color,
                    Icon = icon,
                    IsDefault = true,
                    UserId = user.Id
                });
            }
            await _unitOfWork.SaveChangesAsync();

            var token = GenerateJwtToken(user);

            return new LoginResponseDto
            {
                AccessToken = token,
                User = new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Name = user.Name,
                    Email = user.Email,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt
                }
            };
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto loginRequestDto)
        {
            var user = await _userRepository.GetByUsernameAsync(loginRequestDto.Username);
            // El mismo mensaje para "no existe" y "contraseña mal": si fueran
            // distintos, probando nombres se podria averiguar quien tiene cuenta.
            const string credencialesInvalidas = "Usuario o contraseña incorrectos.";
            if (user == null)
                throw new BusinessRuleException(credencialesInvalidas);

            var passwordValid = BCrypt.Net.BCrypt.Verify(loginRequestDto.Password, user.PasswordHash);
            if (!passwordValid)
                throw new BusinessRuleException(credencialesInvalidas);

            // Va DESPUES de verificar la contraseña. Antes iba primero, y cualquiera
            // podia confirmar que una cuenta desactivada existia sin conocer la clave.
            if (!user.IsActive)
                throw new BusinessRuleException("Esta cuenta está desactivada.");

            var token = GenerateJwtToken(user);

            return new LoginResponseDto
            {
                AccessToken = token,
                User = new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Name = user.Name,
                    Email = user.Email,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt
                }
            };
        }

        // Despues de cambiar la contraseña, el token de quien la cambio ya no sirve
        // (lleva el sello del hash viejo). Se le da uno nuevo para que no lo saque
        // de la sesion; las demas sesiones si quedan cerradas, que es lo buscado.
        public async Task<string> IssueTokenAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId)
                ?? throw new NotFoundException("User", userId);
            return GenerateJwtToken(user);
        }

        private string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(SelloDeSesion.Claim, SelloDeSesion.De(user.PasswordHash))
            };

            var token = new JwtSecurityToken(
                issuer: _jwtIssuer,
                audience: _jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtExpirationMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
