using Application.DTOs.DiaryEntries;        
using Application.Interfaces.Repositories; 
using Application.Interfaces.Services;   
using Domain.Entities;                     

namespace Application.Services
{
    public class DiaryEntryService : IDiaryEntryService
    {
        private readonly IDiaryEntryRepository _diaryEntryRepository;

        public DiaryEntryService(IDiaryEntryRepository diaryEntryRepository)
        {
            _diaryEntryRepository = diaryEntryRepository;
        }

        // devuelve todas las entradas de diario de un usuario, ordenadas por fecha (el repositorio ya las ordena)
        public async Task<IEnumerable<DiaryEntryDto>> GetByUserIdAsync(int userId)
        {
            var entries = await _diaryEntryRepository.GetByUserIdAsync(userId);
            return entries.Select(ToDto);
        }

        // devuelve una entrada por id
        public async Task<DiaryEntryDto?> GetByIdAsync(int id)
        {
            var entry = await _diaryEntryRepository.GetByIdAsync(id);
            return entry is null ? null : ToDto(entry);
        }

        // crea una entrada de diario nueva
        public async Task<DiaryEntryDto> CreateAsync(int userId, CreateDiaryEntryDto dto)
        {
            var entry = new DiaryEntry
            {
                Title = dto.Title,
                Content = dto.Content,
                EntryDate = dto.EntryDate, // fecha que elige el usuario (puede ser distinta a CreatedAt)
                UserId = userId
            };

            var created = await _diaryEntryRepository.CreateAsync(entry);
            return ToDto(created);
        }

        // actualiza una entrada existente
        public async Task<DiaryEntryDto?> UpdateAsync(int id, UpdateDiaryEntryDto dto)
        {
            var entry = await _diaryEntryRepository.GetByIdAsync(id);
            if (entry is null) return null;

            entry.Title = dto.Title;
            entry.Content = dto.Content;
            entry.EntryDate = dto.EntryDate;
            entry.UpdatedAt = DateTime.UtcNow; // marca cuándo fue editada

            var updated = await _diaryEntryRepository.UpdateAsync(entry);
            return ToDto(updated);
        }

        // elimina una entrada
        public async Task<bool> DeleteAsync(int id)
        {
            return await _diaryEntryRepository.DeleteAsync(id);
        }

        // convierte la entidad DiaryEntry al DTO de respuesta
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
