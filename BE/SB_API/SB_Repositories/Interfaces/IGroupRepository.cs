using System.Collections.Generic;
using System.Threading.Tasks;
using SB_BusinessObjects.Entities;

namespace SB_Repositories.Interfaces
{
    public interface IGroupRepository
    {
        Task<Group?> GetByIdAsync(string id);
        Task<Group?> GetByIdWithMembersAsync(string id);
        Task<IEnumerable<Group>> GetGroupsByUserIdAsync(string userId);
        Task AddAsync(Group group);
        Task UpdateAsync(Group group);
        Task DeleteAsync(Group group);
        
        // Thành viên trong nhóm
        Task<GroupMember?> GetMemberAsync(string groupId, string memberId);
        Task<GroupMember?> GetMemberByUserIdAsync(string groupId, string userId);
        Task<IEnumerable<GroupMember>> GetMembersByGroupIdAsync(string groupId);
        Task AddMemberAsync(GroupMember member);
        Task UpdateMemberAsync(GroupMember member);
        Task RemoveMemberAsync(GroupMember member);
    }
}
