using Application.DTOs.Categories;          
using Application.Interfaces.Repositories; 
using Application.Interfaces.Services;     
using Domain.Entities;                  

namespace Application.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;

        // constructor: recibe el repositorio por inyección de dependencias
        public CategoryService(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        // devuelve todas las categorías de un usuario específico
        public async Task<IEnumerable<CategoryDto>> GetByUserIdAsync(int userId)
        {
            var categories = await _categoryRepository.GetByUserIdAsync(userId); // filtra por userId en la DB
            return categories.Select(ToDto);                                      // convierte cada Category → CategoryDto
        }

        // devuelve una categoría por id, o null si no existe
        public async Task<CategoryDto?> GetByIdAsync(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            return category is null ? null : ToDto(category);
        }

        // crea una categoría nueva para el usuario indicado
        public async Task<CategoryDto> CreateAsync(int userId, CreateCategoryDto dto)
        {
            var category = new Category
            {
                Name = dto.Name,
                Color = dto.Color,
                Icon = dto.Icon,
                IsDefault = dto.IsDefault,
                UserId = userId // el userId viene del controller, no del DTO, por seguridad
            };

            var created = await _categoryRepository.CreateAsync(category);
            return ToDto(created);
        }

        // actualiza una categoría existente
        public async Task<CategoryDto?> UpdateAsync(int id, UpdateCategoryDto dto)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category is null) return null; // si no existe, devuelve null

            // actualiza solo los campos editables
            category.Name = dto.Name;
            category.Color = dto.Color;
            category.Icon = dto.Icon;
            category.IsDefault = dto.IsDefault;
            // no actualiza UserId ni CreatedAt porque esos nunca cambian

            var updated = await _categoryRepository.UpdateAsync(category);
            return ToDto(updated);
        }

        // elimina una categoría
        public async Task<bool> DeleteAsync(int id)
        {
            return await _categoryRepository.DeleteAsync(id);
        }

        // convierte la entidad Category al DTO de respuesta
        private static CategoryDto ToDto(Category c) => new()
        {
            Id = c.Id,
            Name = c.Name,
            Color = c.Color,
            Icon = c.Icon,
            IsDefault = c.IsDefault,
            UserId = c.UserId,
            CreatedAt = c.CreatedAt
        };
    }
}
