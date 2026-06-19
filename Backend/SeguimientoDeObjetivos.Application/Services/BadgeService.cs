using Application.DTOs.Badges;              
using Application.Interfaces.Repositories; 
using Application.Interfaces.Services;     

namespace Application.Services
{
    public class BadgeService : IBadgeService
    {
        private readonly IBadgeRepository _badgeRepository;

        public BadgeService(IBadgeRepository badgeRepository)
        {
            _badgeRepository = badgeRepository;
        }

        // devuelve todos los badges del sistema (los ve cualquier usuario)
        public async Task<IEnumerable<BadgeDto>> GetAllAsync()
        {
            var badges = await _badgeRepository.GetAllAsync();
            return badges.Select(ToDto);
        }

        // devuelve un badge por id
        public async Task<BadgeDto?> GetByIdAsync(int id)
        {
            var badge = await _badgeRepository.GetByIdAsync(id);
            return badge is null ? null : ToDto(badge);
        }

        // devuelve los badges que ya ganó un usuario específico
        public async Task<IEnumerable<BadgeDto>> GetByUserIdAsync(int userId)
        {
            var badges = await _badgeRepository.GetByUserIdAsync(userId);
            return badges.Select(ToDto);
        }

        // asigna un badge a un usuario
        // devuelve false si el usuario ya tiene ese badge (el repositorio verifica duplicados)
        public async Task<bool> AssignToUserAsync(int userId, int badgeId)
        {
            return await _badgeRepository.AssignToUserAsync(userId, badgeId);
        }

        // convierte la entidad Badge al DTO de respuesta
        // no hay entidad que importar porque Badge viene de Domain.Entities
        // y con ImplicitUsings está disponible desde el mismo namespace del proyecto
        private static BadgeDto ToDto(Domain.Entities.Badge b) => new()
        {
            Id = b.Id,
            Name = b.Name,
            Description = b.Description,
            BadgeType = b.BadgeType,
            Icon = b.Icon,
            CreatedAt = b.CreatedAt
        };
    }
}
