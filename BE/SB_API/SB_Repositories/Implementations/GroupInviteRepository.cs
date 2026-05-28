using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SB_BusinessObjects;
using SB_BusinessObjects.Entities;
using SB_Repositories.Interfaces;

namespace SB_Repositories.Implementations
{
    public class GroupInviteRepository : IGroupInviteRepository
    {
        private readonly AppDbContext _context;

        public GroupInviteRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(GroupInvite invite)
        {
            await _context.GroupInvites.AddAsync(invite);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(GroupInvite invite)
        {
            _context.GroupInvites.Update(invite);
            await _context.SaveChangesAsync();
        }

        public async Task<GroupInvite?> GetByTokenAsync(string token)
        {
            return await _context.GroupInvites
                .Include(i => i.Group)
                .FirstOrDefaultAsync(i => i.Token == token);
        }

        public async Task<IEnumerable<GroupInvite>> GetByGroupIdAsync(string groupId)
        {
            return await _context.GroupInvites
                .Where(i => i.GroupId == groupId)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }
    }
}
