using Application.DTOs.Categories;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;

namespace Application.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CategoryService(ICategoryRepository categoryRepository, IUnitOfWork unitOfWork)
        {
            _categoryRepository = categoryRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<CategoryDto>> GetByUserIdAsync(int userId)
        {
            var categories = await _categoryRepository.GetByUserIdAsync(userId);
            return categories.Select(ToDto);
        }

        public async Task<CategoryDto?> GetByIdAsync(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            return category is null ? null : ToDto(category);
        }

        public async Task<CategoryDto> CreateAsync(int userId, CreateCategoryDto dto)
        {
            var category = new Category
            {
                Name = dto.Name,
                Color = dto.Color,
                Icon = dto.Icon,
                IsDefault = dto.IsDefault,
                UserId = userId
            };

            var created = await _categoryRepository.CreateAsync(category);
            await _unitOfWork.SaveChangesAsync();
            return ToDto(created);
        }

        public async Task<CategoryDto?> UpdateAsync(int id, UpdateCategoryDto dto)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category is null) return null;

            category.Name = dto.Name;
            category.Color = dto.Color;
            category.Icon = dto.Icon;
            category.IsDefault = dto.IsDefault;

            var updated = await _categoryRepository.UpdateAsync(category);
            await _unitOfWork.SaveChangesAsync();
            return ToDto(updated);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var deleted = await _categoryRepository.DeleteAsync(id);
            if (!deleted) return false;
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

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
