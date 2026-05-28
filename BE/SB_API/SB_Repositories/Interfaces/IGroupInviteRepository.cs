using System.Threading.Tasks;
using System.Collections.Generic;
using SB_BusinessObjects.Entities;

namespace SB_Repositories.Interfaces
{
    public interface IGroupInviteRepository
    {
        Task AddAsync(GroupInvite invite);
        Task UpdateAsync(GroupInvite invite);
        Task<GroupInvite?> GetByTokenAsync(string token);
        Task<IEnumerable<GroupInvite>> GetByGroupIdAsync(string groupId);
    }
}
