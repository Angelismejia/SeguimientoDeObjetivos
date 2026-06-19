using Application.DTOs.Users;              
using Application.Interfaces.Repositories; 
using Application.Interfaces.Services;     
using Domain.Entities;                   

namespace Application.Services
{
    public class UserService : IUserService
    {
       
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository; 
        }

        // devuelve todos los usuarios convertidos a DTO
        public async Task<IEnumerable<UserDto>> GetAllAsync()
        {
            var users = await _userRepository.GetAllAsync(); 
            return users.Select(ToDto);                      // convierte cada User → UserDto
        }

        // devuelve un usuario por id, o null si no existe
        public async Task<UserDto?> GetByIdAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id); // busca en la DB

            return user is null ? null : ToDto(user);           // si no existe devuelve null, si existe convierte a DTO
        }

        // crea un usuario nuevo y devuelve el DTO con el Id generado por la DB
        public async Task<UserDto> CreateAsync(CreateUserDto dto)
        {
            // crea la entidad con los datos que mandó el cliente
            // no incluye Id (lo asigna la DB), ni CreatedAt (valor por defecto en la entidad)
            var user = new User
            {
                KeycloakUserId = dto.KeycloakUserId,
                Name = dto.Name,
                Email = dto.Email
            };

            var created = await _userRepository.CreateAsync(user); // guarda en la DB
            return ToDto(created);                                  // devuelve el usuario creado como DTO
        }

        // actualiza un usuario existente, devuelve null si no existe
        public async Task<UserDto?> UpdateAsync(int id, UpdateUserDto dto)
        {
            var user = await _userRepository.GetByIdAsync(id); // busca el usuario
            if (user is null) return null;                      // si no existe, sale sin hacer nada

            // sobreescribe solo los campos que el cliente puede cambiar
            user.Name = dto.Name;
            user.Email = dto.Email;
            user.IsActive = dto.IsActive;
            user.UpdatedAt = DateTime.UtcNow; // marca la fecha de modificación

            var updated = await _userRepository.UpdateAsync(user); // guarda los cambios en la DB
            return ToDto(updated);                                  // devuelve el usuario actualizado como DTO
        }

        // elimina un usuario, devuelve true si existía y se borró, false si no existía
        public async Task<bool> DeleteAsync(int id)
        {
            return await _userRepository.DeleteAsync(id); // delega directamente al repositorio
        }

        // método privado que convierte una entidad User en un UserDto
        // static porque no necesita acceder a ningún campo de la clase
        private static UserDto ToDto(User u) => new()
        {
            Id = u.Id,
            KeycloakUserId = u.KeycloakUserId,
            Name = u.Name,
            Email = u.Email,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt,
            UpdatedAt = u.UpdatedAt
        };
    }
}
