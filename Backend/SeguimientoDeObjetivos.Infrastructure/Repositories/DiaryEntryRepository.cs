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

        // El diario se ordena por EntryDate, que elige el usuario: no sigue el orden
        // de los ids y puede repetirse en el mismo dia. Por eso el cursor es el par
        // (fecha, id), y el id desempata dentro del mismo dia.
        public async Task<IEnumerable<DiaryEntry>> GetPageByUserIdAsync(int userId, int limit, DateTime? beforeDate, int? beforeId)
        {
            var query = _context.DiaryEntries.Where(d => d.UserId == userId);

            if (beforeDate is not null && beforeId is not null)
                query = query.Where(d => d.EntryDate < beforeDate
                    || (d.EntryDate == beforeDate && d.Id < beforeId));

            return await query
                .OrderByDescending(d => d.EntryDate)
                .ThenByDescending(d => d.Id)
                .Take(limit)
                .ToListAsync();
        }

        public Task<int> CountByUserIdAsync(int userId)
            => _context.DiaryEntries.CountAsync(d => d.UserId == userId);

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
