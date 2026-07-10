using Application.DTOs.Users;
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
                throw new InvalidOperationException("Username already exists");

            var existingEmail = await _userRepository.GetByEmailAsync(createUserDto.Email);
            if (existingEmail != null)
                throw new InvalidOperationException("Email already exists");

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
            if (user == null)
                throw new InvalidOperationException("Invalid username or password");

            if (!user.IsActive)
                throw new InvalidOperationException("User account is inactive");

            var passwordValid = BCrypt.Net.BCrypt.Verify(loginRequestDto.Password, user.PasswordHash);
            if (!passwordValid)
                throw new InvalidOperationException("Invalid username or password");

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

        private string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email)
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
