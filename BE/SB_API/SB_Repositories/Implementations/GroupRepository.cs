using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SB_BusinessObjects;
using SB_BusinessObjects.Entities;
using SB_Repositories.Interfaces;

namespace SB_Repositories.Implementations
{
    public class GroupRepository : IGroupRepository
    {
        private readonly AppDbContext _context;

        public GroupRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Group?> GetByIdAsync(string id)
        {
            return await _context.Groups.FirstOrDefaultAsync(g => g.Id == id);
        }

        public async Task<Group?> GetByIdWithMembersAsync(string id)
        {
            return await _context.Groups
                .Include(g => g.Members)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(g => g.Id == id);
        }

        public async Task<IEnumerable<Group>> GetGroupsByUserIdAsync(string userId)
        {
            return await _context.Groups
                .Where(g => g.CreatedById == userId || g.Members.Any(m => m.UserId == userId))
                .OrderByDescending(g => g.CreatedAt)
                .ToListAsync();
        }

        public async Task AddAsync(Group group)
        {
            await _context.Groups.AddAsync(group);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Group group)
        {
            _context.Groups.Update(group);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Group group)
        {
            _context.Groups.Remove(group);
            await _context.SaveChangesAsync();
        }

        public async Task<GroupMember?> GetMemberAsync(string groupId, string memberId)
        {
            return await _context.GroupMembers
                .FirstOrDefaultAsync(m => m.GroupId == groupId && m.Id == memberId);
        }

        public async Task<GroupMember?> GetMemberByUserIdAsync(string groupId, string userId)
        {
            return await _context.GroupMembers
                .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId);
        }

        public async Task<IEnumerable<GroupMember>> GetMembersByGroupIdAsync(string groupId)
        {
            return await _context.GroupMembers
                .Where(m => m.GroupId == groupId)
                .ToListAsync();
        }

        public async Task AddMemberAsync(GroupMember member)
        {
            await _context.GroupMembers.AddAsync(member);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateMemberAsync(GroupMember member)
        {
            _context.GroupMembers.Update(member);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveMemberAsync(GroupMember member)
        {
            _context.GroupMembers.Remove(member);
            await _context.SaveChangesAsync();
        }
    }
}
