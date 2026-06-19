using Application.DTOs.DiaryEntries;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;

namespace Application.Services
{
    public class DiaryEntryService : IDiaryEntryService
    {
        private readonly IDiaryEntryRepository _diaryEntryRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DiaryEntryService(IDiaryEntryRepository diaryEntryRepository, IUnitOfWork unitOfWork)
        {
            _diaryEntryRepository = diaryEntryRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<DiaryEntryDto>> GetByUserIdAsync(int userId)
        {
            var entries = await _diaryEntryRepository.GetByUserIdAsync(userId);
            return entries.Select(ToDto);
        }

        public async Task<DiaryEntryDto?> GetByIdAsync(int id)
        {
            var entry = await _diaryEntryRepository.GetByIdAsync(id);
            return entry is null ? null : ToDto(entry);
        }

        public async Task<DiaryEntryDto> CreateAsync(int userId, CreateDiaryEntryDto dto)
        {
            var entry = new DiaryEntry
            {
                Title = dto.Title,
                Content = dto.Content,
                EntryDate = dto.EntryDate,
                UserId = userId
            };

            var created = await _diaryEntryRepository.CreateAsync(entry);
            await _unitOfWork.SaveChangesAsync();
            return ToDto(created);
        }

        public async Task<DiaryEntryDto?> UpdateAsync(int id, UpdateDiaryEntryDto dto)
        {
            var entry = await _diaryEntryRepository.GetByIdAsync(id);
            if (entry is null) return null;

            entry.Title = dto.Title;
            entry.Content = dto.Content;
            entry.EntryDate = dto.EntryDate;
            entry.UpdatedAt = DateTime.UtcNow;

            var updated = await _diaryEntryRepository.UpdateAsync(entry);
            await _unitOfWork.SaveChangesAsync();
            return ToDto(updated);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var deleted = await _diaryEntryRepository.DeleteAsync(id);
            if (!deleted) return false;
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        private static DiaryEntryDto ToDto(DiaryEntry d) => new()
        {
            Id = d.Id,
            Title = d.Title,
            Content = d.Content,
            EntryDate = d.EntryDate,
            UserId = d.UserId,
            CreatedAt = d.CreatedAt,
            UpdatedAt = d.UpdatedAt
        };
    }
}
