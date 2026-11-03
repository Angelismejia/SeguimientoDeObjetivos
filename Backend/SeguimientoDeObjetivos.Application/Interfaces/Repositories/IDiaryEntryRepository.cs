using Domain.Entities;

namespace Application.Interfaces.Repositories
{
    public interface IDiaryEntryRepository
    {
        Task<DiaryEntry?> GetByIdAsync(int id);
        Task<IEnumerable<DiaryEntry>> GetByUserIdAsync(int userId);
        Task<IEnumerable<DiaryEntry>> GetPageByUserIdAsync(int userId, int limit, DateTime? beforeDate, int? beforeId);
        Task<int> CountByUserIdAsync(int userId);
        Task<DiaryEntry> CreateAsync(DiaryEntry entry);
        Task<DiaryEntry> UpdateAsync(DiaryEntry entry);
        Task<bool> DeleteAsync(int id);
    }
}
