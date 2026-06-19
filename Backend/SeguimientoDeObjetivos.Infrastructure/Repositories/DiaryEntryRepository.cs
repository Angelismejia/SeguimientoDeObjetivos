using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class DiaryEntryRepository : IDiaryEntryRepository
    {
        private readonly ApplicationDbContext _context;

        public DiaryEntryRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DiaryEntry?> GetByIdAsync(int id)
            => await _context.DiaryEntries.FindAsync(id);

        public async Task<IEnumerable<DiaryEntry>> GetByUserIdAsync(int userId)
            => await _context.DiaryEntries
                .Where(d => d.UserId == userId)
                .OrderByDescending(d => d.EntryDate)
                .ToListAsync();

        public Task<DiaryEntry> CreateAsync(DiaryEntry entry)
        {
            _context.DiaryEntries.Add(entry);
            return Task.FromResult(entry);
        }

        public Task<DiaryEntry> UpdateAsync(DiaryEntry entry)
        {
            _context.DiaryEntries.Update(entry);
            return Task.FromResult(entry);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entry = await _context.DiaryEntries.FindAsync(id);
            if (entry is null) return false;
            _context.DiaryEntries.Remove(entry);
            return true;
        }
    }
}
