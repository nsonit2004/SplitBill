using System.Collections.Generic;
using System.Threading.Tasks;
using SB_Services.DTOs;

namespace SB_Services.Interfaces
{
    public interface IGroupService
    {
        Task<GroupDetailResponseDto> CreateGroupAsync(CreateGroupRequestDto request, string creatorUserId);
        Task<GroupDetailResponseDto> GetGroupDetailAsync(string groupId);
        Task<IEnumerable<GroupDetailResponseDto>> GetUserGroupsAsync(string userId);
        Task<GroupMemberDto> AddVirtualMemberAsync(string groupId, string nickname);
        Task<GroupMemberDto> LinkMemberAccountAsync(string groupId, string memberId, string userId);
        Task RemoveMemberAsync(string groupId, string memberId);
    }
}
