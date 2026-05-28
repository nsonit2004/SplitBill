using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SB_BusinessObjects.Entities;
using SB_Repositories.Interfaces;
using SB_Services.DTOs;
using SB_Services.Interfaces;

namespace SB_Services.Implementations
{
    public class GroupService : IGroupService
    {
        private readonly IGroupRepository _groupRepository;
        private readonly IUserRepository _userRepository;

        public GroupService(IGroupRepository groupRepository, IUserRepository userRepository)
        {
            _groupRepository = groupRepository;
            _userRepository = userRepository;
        }

        public async Task<GroupDetailResponseDto> CreateGroupAsync(CreateGroupRequestDto request, string creatorUserId)
        {
            var creator = await _userRepository.GetByIdAsync(creatorUserId);
            if (creator == null)
            {
                throw new ArgumentException("Tài khoản tạo nhóm không tồn tại.");
            }

            var group = new Group
            {
                Name = request.Name,
                Description = request.Description,
                CreatedById = creatorUserId
            };

            // Thêm trưởng nhóm (người tạo) vào danh sách thành viên trước
            var creatorMember = new GroupMember
            {
                GroupId = group.Id,
                UserId = creatorUserId,
                Nickname = creator.DisplayName,
                IsVirtual = false
            };
            group.Members.Add(creatorMember);

            // Thêm các thành viên ảo ban đầu
            if (request.Members != null)
            {
                foreach (var nickname in request.Members)
                {
                    if (string.IsNullOrWhiteSpace(nickname)) continue;

                    group.Members.Add(new GroupMember
                    {
                        GroupId = group.Id,
                        Nickname = nickname,
                        IsVirtual = true
                    });
                }
            }

            await _groupRepository.AddAsync(group);

            return MapToDetailDto(group);
        }

        public async Task<GroupDetailResponseDto> GetGroupDetailAsync(string groupId)
        {
            var group = await _groupRepository.GetByIdWithMembersAsync(groupId);
            if (group == null)
            {
                throw new KeyNotFoundException("Không tìm thấy nhóm chi tiêu.");
            }

            return MapToDetailDto(group);
        }

        public async Task<IEnumerable<GroupDetailResponseDto>> GetUserGroupsAsync(string userId)
        {
            var groups = await _groupRepository.GetGroupsByUserIdAsync(userId);
            var result = new List<GroupDetailResponseDto>();
            
            foreach (var group in groups)
            {
                // Load thành viên của từng nhóm để hiển thị đầy đủ thông tin
                var fullGroup = await _groupRepository.GetByIdWithMembersAsync(group.Id);
                if (fullGroup != null)
                {
                    result.Add(MapToDetailDto(fullGroup));
                }
            }

            return result;
        }

        public async Task<GroupMemberDto> AddVirtualMemberAsync(string groupId, string nickname)
        {
            var group = await _groupRepository.GetByIdAsync(groupId);
            if (group == null)
            {
                throw new KeyNotFoundException("Không tìm thấy nhóm chi tiêu.");
            }

            if (string.IsNullOrWhiteSpace(nickname))
            {
                throw new ArgumentException("Tên thành viên không được để trống.");
            }

            var newMember = new GroupMember
            {
                GroupId = groupId,
                Nickname = nickname,
                IsVirtual = true
            };

            await _groupRepository.AddMemberAsync(newMember);

            return new GroupMemberDto
            {
                Id = newMember.Id,
                UserId = null,
                Nickname = newMember.Nickname,
                IsVirtual = newMember.IsVirtual,
                JoinedAt = newMember.JoinedAt
            };
        }

        public async Task<GroupMemberDto> LinkMemberAccountAsync(string groupId, string memberId, string userId)
        {
            var member = await _groupRepository.GetMemberAsync(groupId, memberId);
            if (member == null)
            {
                throw new KeyNotFoundException("Không tìm thấy thành viên trong nhóm.");
            }

            if (!member.IsVirtual || member.UserId != null)
            {
                throw new InvalidOperationException("Thành viên này đã liên kết tài khoản rồi.");
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new KeyNotFoundException("Tài khoản liên kết không tồn tại.");
            }

            // Kiểm tra xem user này đã là thành viên trong nhóm dưới dạng một bản ghi khác chưa
            var existingUserMember = await _groupRepository.GetMemberByUserIdAsync(groupId, userId);
            if (existingUserMember != null)
            {
                throw new InvalidOperationException("Tài khoản này đã là thành viên của nhóm.");
            }

            // Thực hiện liên kết tài khoản
            member.UserId = userId;
            member.IsVirtual = false;
            // Cập nhật Nickname theo tên hiển thị của user (hoặc giữ lại nickname tùy ý)
            member.Nickname = user.DisplayName;

            await _groupRepository.UpdateMemberAsync(member);

            return new GroupMemberDto
            {
                Id = member.Id,
                UserId = member.UserId,
                Nickname = member.Nickname,
                IsVirtual = member.IsVirtual,
                JoinedAt = member.JoinedAt
            };
        }

        public async Task RemoveMemberAsync(string groupId, string memberId)
        {
            var member = await _groupRepository.GetMemberAsync(groupId, memberId);
            if (member == null)
            {
                throw new KeyNotFoundException("Không tìm thấy thành viên trong nhóm.");
            }

            // TODO: Kiểm tra xem thành viên này có hóa đơn chưa thanh toán hoặc nợ chưa tất toán không
            // Để đơn giản, ta cho phép xóa, nhưng trong thực tế nên kiểm tra số dư nợ NetBalance = 0 mới được xóa.
            await _groupRepository.RemoveMemberAsync(member);
        }

        private GroupDetailResponseDto MapToDetailDto(Group group)
        {
            return new GroupDetailResponseDto
            {
                Id = group.Id,
                Name = group.Name,
                Description = group.Description,
                CreatedById = group.CreatedById,
                CreatedAt = group.CreatedAt,
                Members = group.Members.Select(m => new GroupMemberDto
                {
                    Id = m.Id,
                    UserId = m.UserId,
                    Nickname = m.Nickname,
                    IsVirtual = m.IsVirtual,
                    JoinedAt = m.JoinedAt
                }).ToList()
            };
        }
    }
}
