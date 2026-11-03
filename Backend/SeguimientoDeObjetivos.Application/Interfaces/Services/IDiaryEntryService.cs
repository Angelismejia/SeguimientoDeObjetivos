using Application.DTOs.DiaryEntries;

namespace Application.Interfaces.Services
{
    public interface IDiaryEntryService
    {
        Task<IEnumerable<DiaryEntryDto>> GetByUserIdAsync(int userId);
        Task<IEnumerable<DiaryEntryDto>> GetPageByUserIdAsync(int userId, int limit, DateTime? beforeDate, int? beforeId);
        Task<int> CountByUserIdAsync(int userId);
        Task<DiaryEntryDto> GetByIdAsync(int id);
        Task<DiaryEntryDto> CreateAsync(int userId, CreateDiaryEntryDto dto);
        Task<DiaryEntryDto> UpdateAsync(int id, UpdateDiaryEntryDto dto);
        Task DeleteAsync(int id);
    }
}
