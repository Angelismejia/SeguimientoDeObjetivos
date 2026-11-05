using Domain.Entities;

namespace Application.Interfaces.Repositories
{
    public interface IObjectiveRepository
    {
        Task<Objective?> GetByIdAsync(int id);
        Task<IEnumerable<Objective>> GetByUserIdAsync(int userId);
        Task<IEnumerable<Objective>> GetPublicFeedAsync(IEnumerable<int> userIds, int limit);
        Task<Objective> CreateAsync(Objective objective);
        Task<Objective> UpdateAsync(Objective objective);
        Task<bool> DeleteAsync(int id);
    }
}
