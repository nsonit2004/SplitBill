using System.Collections.Generic;
using System.Threading.Tasks;
using SB_Services.DTOs;

namespace SB_Services.Interfaces
{
    public interface IGroupService
    {
        Task<GroupDetailResponseDto> CreateGroupAsync(CreateGroupRequestDto request, string creatorUserId);
        Task<GroupDetailResponseDto> GetGroupDetailAsync(string groupId, string? currentUserId = null);
        Task<IEnumerable<GroupDetailResponseDto>> GetUserGroupsAsync(string userId);
        Task<GroupMemberDto> AddVirtualMemberAsync(string groupId, string nickname, string requesterUserId);
        Task<GroupMemberDto> LinkMemberAccountAsync(string groupId, string memberId, string userId);
        Task<GroupMemberDto> LinkMemberAccountByEmailAsync(string groupId, string memberId, string email, string requesterUserId);
        Task<GroupInviteResponseDto> CreateInviteAsync(string groupId, string requesterUserId, CreateGroupInviteRequestDto? request = null);
        Task<IEnumerable<GroupInviteResponseDto>> GetInvitesAsync(string groupId, string requesterUserId);
        Task<GroupInviteResponseDto> RevokeInviteAsync(string groupId, string inviteToken, string requesterUserId);
        Task<GroupDetailResponseDto> AcceptInviteAsync(string inviteToken, string userId);
        Task RemoveMemberAsync(string groupId, string memberId, string requesterUserId);
    }
}
