using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class FriendStreakInvitationRepository : IFriendStreakInvitationRepository
    {
        private readonly ApplicationDbContext _context;

        public FriendStreakInvitationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<FriendStreakInvitation?> GetByIdAsync(int id)
            => await _context.FriendStreakInvitations.FindAsync(id);

        public async Task<FriendStreakInvitation?> GetPendingBetweenAsync(int userAId, int userBId)
            => await _context.FriendStreakInvitations
                .FirstOrDefaultAsync(i => i.Status == "Pending" &&
                    ((i.FromUserId == userAId && i.ToUserId == userBId) ||
                     (i.FromUserId == userBId && i.ToUserId == userAId)));

        public async Task<IEnumerable<FriendStreakInvitation>> GetReceivedAsync(int userId)
            => await _context.FriendStreakInvitations
                .Include(i => i.FromUser)
                .Where(i => i.ToUserId == userId && i.Status == "Pending")
                .ToListAsync();

        public async Task<IEnumerable<FriendStreakInvitation>> GetSentAsync(int userId)
            => await _context.FriendStreakInvitations
                .Include(i => i.ToUser)
                .Where(i => i.FromUserId == userId && i.Status == "Pending")
                .ToListAsync();

        public Task<FriendStreakInvitation> CreateAsync(FriendStreakInvitation invitation)
        {
            _context.FriendStreakInvitations.Add(invitation);
            return Task.FromResult(invitation);
        }

        public Task<FriendStreakInvitation> UpdateAsync(FriendStreakInvitation invitation)
        {
            _context.FriendStreakInvitations.Update(invitation);
            return Task.FromResult(invitation);
        }
    }
}
