using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly ApplicationDbContext _context;

        public CategoryRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Category?> GetByIdAsync(int id)
            => await _context.Categories.FindAsync(id);

        public async Task<IEnumerable<Category>> GetByUserIdAsync(int userId)
            => await _context.Categories
                .Where(c => c.UserId == userId)
                .ToListAsync();

        public Task<Category> CreateAsync(Category category)
        {
            _context.Categories.Add(category);
            return Task.FromResult(category);
        }

        public Task<Category> UpdateAsync(Category category)
        {
            _context.Categories.Update(category);
            return Task.FromResult(category);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category is null) return false;

            // Ni Objectives.CategoryId ni Tasks.CategoryId cascadean, asi que
            // borrar una categoria en uso chocaba contra la FK. Ambas son
            // opcionales: lo que la usaba queda sin categoria en vez de
            // bloquear el borrado o arrastrar objetivos y tareas.
            var objectives = await _context.Objectives
                .Where(o => o.CategoryId == id)
                .ToListAsync();
            foreach (var objective in objectives)
                objective.CategoryId = null;

            var tasks = await _context.Tasks
                .Where(t => t.CategoryId == id)
                .ToListAsync();
            foreach (var task in tasks)
                task.CategoryId = null;

            _context.Categories.Remove(category);
            return true;
        }
    }
}
