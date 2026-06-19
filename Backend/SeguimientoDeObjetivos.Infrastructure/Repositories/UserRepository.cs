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

        public async Task<User?> GetByKeycloakIdAsync(string keycloakUserId)
            => await _context.Users.FirstOrDefaultAsync(u => u.KeycloakUserId == keycloakUserId);

        public async Task<IEnumerable<User>> GetAllAsync()
            => await _context.Users.ToListAsync();

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
