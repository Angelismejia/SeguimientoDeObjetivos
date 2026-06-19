using Domain.Entities;


// Repositorio para gestionar las insignias (badges) de los usuarios
namespace Application.Interfaces.Repositories
{
    public interface IBadgeRepository


    {
        // Métodos para gestionar las insignias
        Task<Badge?> GetByIdAsync(int id);
        // Obtener todas las insignias disponibles
        Task<IEnumerable<Badge>> GetAllAsync();
        // Obtener las insignias asignadas a un usuario específico
        Task<IEnumerable<Badge>> GetByUserIdAsync(int userId);
        // Asignar una insignia a un usuario
        // Devuelve true si la asignación fue exitosa, false si el usuario o la insignia no existen
        Task<bool> AssignToUserAsync(int userId, int badgeId);
    }
}
