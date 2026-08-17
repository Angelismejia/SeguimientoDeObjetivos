using Application.Interfaces;
using Application.Interfaces.Repositories;
using Domain.Entities;

namespace SeguimientoDeObjetivos.Tests;

// Dobles de prueba escritos a mano en vez de una libreria de mocks: solo hacen
// falta unos pocos metodos con comportamiento real, el resto nunca se llama.

internal sealed class RelojFijo : IAppClock
{
    private readonly DateTime _hoy;
    public RelojFijo(DateTime hoy) => _hoy = hoy.Date;
    public DateTime Now => _hoy;
    public DateTime Today => _hoy;
    public DateTime ToUtc(DateTime local) => local;
}

internal sealed class UnitOfWorkFalso : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(0);
}

internal sealed class TaskRepoFalso : ITaskRepository
{
    private readonly Dictionary<int, List<TaskItem>> _porUsuario;
    public TaskRepoFalso(Dictionary<int, List<TaskItem>> porUsuario) => _porUsuario = porUsuario;

    public Task<IEnumerable<TaskItem>> GetByUserIdAsync(int userId) =>
        Task.FromResult<IEnumerable<TaskItem>>(
            _porUsuario.TryGetValue(userId, out var t) ? t : new List<TaskItem>());

    public Task<TaskItem?> GetByIdAsync(int id) => throw new NotSupportedException();
    public Task<IEnumerable<TaskItem>> GetByObjectiveIdAsync(int objectiveId) => throw new NotSupportedException();
    public Task<TaskItem> CreateAsync(TaskItem task) => throw new NotSupportedException();
    public Task<TaskItem> UpdateAsync(TaskItem task) => throw new NotSupportedException();
    public Task<bool> DeleteAsync(int id) => throw new NotSupportedException();
}

internal sealed class CompletionRepoFalso : ITaskCompletionRepository
{
    private readonly Dictionary<int, List<TaskCompletion>> _porUsuario;
    public CompletionRepoFalso(Dictionary<int, List<TaskCompletion>> porUsuario) => _porUsuario = porUsuario;

    public Task<IEnumerable<TaskCompletion>> GetByUserIdAsync(int userId) =>
        Task.FromResult<IEnumerable<TaskCompletion>>(
            _porUsuario.TryGetValue(userId, out var c) ? c : new List<TaskCompletion>());

    public Task<IEnumerable<TaskCompletion>> GetByTaskIdAsync(int taskId) => throw new NotSupportedException();
    public Task<TaskCompletion?> GetAsync(int taskId, DateTime date) => throw new NotSupportedException();
    public Task<TaskCompletion> CreateAsync(TaskCompletion completion) => throw new NotSupportedException();
    public Task<bool> DeleteAsync(int taskId, DateTime date) => throw new NotSupportedException();
}

internal sealed class FriendStreakRepoFalso : IFriendStreakRepository
{
    private readonly List<FriendStreak> _rachas;
    public FriendStreakRepoFalso(params FriendStreak[] rachas) => _rachas = rachas.ToList();

    public Task<IEnumerable<FriendStreak>> GetForUserAsync(int userId) =>
        Task.FromResult<IEnumerable<FriendStreak>>(
            _rachas.Where(f => f.UserAId == userId || f.UserBId == userId).ToList());

    public Task<FriendStreak?> GetAsync(int a, int b) => throw new NotSupportedException();
    public Task<FriendStreak> CreateAsync(FriendStreak f) => throw new NotSupportedException();
    public Task DeleteAllForUserAsync(int userId) => throw new NotSupportedException();
}

internal sealed class UserRepoFalso : IUserRepository
{
    public Task<User?> GetByIdAsync(int id) =>
        Task.FromResult<User?>(new User { Id = id, Username = $"user{id}", Name = $"Usuario {id}" });

    public Task<IEnumerable<User>> GetAllAsync() => throw new NotSupportedException();
    public Task<User?> GetByUsernameAsync(string username) => throw new NotSupportedException();
    public Task<User?> GetByEmailAsync(string email) => throw new NotSupportedException();
    public void Add(User user) => throw new NotSupportedException();
    public Task<User> CreateAsync(User user) => throw new NotSupportedException();
    public Task<User> UpdateAsync(User user) => throw new NotSupportedException();
    public Task<bool> DeleteAsync(int id) => throw new NotSupportedException();
    public Task<bool> ExistsAsync(int id) => throw new NotSupportedException();
}

internal sealed class FollowRepoFalso : IFollowRepository
{
    public Task<Follow?> GetByIdAsync(int id) => throw new NotSupportedException();
    public Task<Follow?> GetAsync(int a, int b) => throw new NotSupportedException();
    public Task<IEnumerable<Follow>> GetFollowersAsync(int userId) => throw new NotSupportedException();
    public Task<IEnumerable<Follow>> GetFollowingAsync(int userId) => throw new NotSupportedException();
    public Task<Follow> CreateAsync(Follow follow) => throw new NotSupportedException();
    public Task<bool> DeleteAsync(int a, int b) => throw new NotSupportedException();
    public Task DeleteAllForUserAsync(int userId) => throw new NotSupportedException();
}

internal sealed class InvitationRepoFalso : IFriendStreakInvitationRepository
{
    public Task<FriendStreakInvitation?> GetByIdAsync(int id) => throw new NotSupportedException();
    public Task<FriendStreakInvitation?> GetPendingBetweenAsync(int a, int b) => throw new NotSupportedException();
    public Task<IEnumerable<FriendStreakInvitation>> GetReceivedAsync(int userId) => throw new NotSupportedException();
    public Task<IEnumerable<FriendStreakInvitation>> GetSentAsync(int userId) => throw new NotSupportedException();
    public Task<FriendStreakInvitation> CreateAsync(FriendStreakInvitation i) => throw new NotSupportedException();
    public Task<FriendStreakInvitation> UpdateAsync(FriendStreakInvitation i) => throw new NotSupportedException();
    public Task DeleteAllForUserAsync(int userId) => throw new NotSupportedException();
}
