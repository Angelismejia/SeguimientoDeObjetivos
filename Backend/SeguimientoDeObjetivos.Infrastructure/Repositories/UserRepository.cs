using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByIdAsync(int id)
            => await _context.Users.FindAsync(id);

        public async Task<User?> GetByUsernameAsync(string username)
            => await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

        public async Task<User?> GetByEmailAsync(string email)
            => await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        public async Task<IEnumerable<User>> GetAllAsync()
            => await _context.Users.ToListAsync();

        // Las cuentas desactivadas no aparecen: no tiene sentido ofrecer seguir a
        // alguien que ya no usa la app. ILIKE es el "contiene" sin distinguir
        // mayusculas de PostgreSQL; % y _ se escapan para que se busquen literales.
        public async Task<IEnumerable<User>> SearchActiveAsync(string? query, int limit)
        {
            var users = _context.Users.Where(u => u.IsActive);
            if (!string.IsNullOrEmpty(query))
            {
                var patron = "%" + query.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
                users = users.Where(u => EF.Functions.ILike(u.Username, patron) || EF.Functions.ILike(u.Name, patron));
            }
            return await users.OrderBy(u => u.Name).Take(limit).ToListAsync();
        }

        public void Add(User user)
            => _context.Users.Add(user);

        public Task<User> CreateAsync(User user)
        {
            _context.Users.Add(user);
            return Task.FromResult(user);
        }

        public Task<User> UpdateAsync(User user)
        {
            _context.Users.Update(user);
            return Task.FromResult(user);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user is null) return false;
            _context.Users.Remove(user);
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
            => await _context.Users.AnyAsync(u => u.Id == id);
    }
}
