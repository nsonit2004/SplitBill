using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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
        private readonly IExpenseRepository _expenseRepository;
        private readonly ISettleTransactionRepository _transactionRepository;
        private readonly IGroupInviteRepository _groupInviteRepository;

        public GroupService(
            IGroupRepository groupRepository, 
            IUserRepository userRepository,
            IExpenseRepository expenseRepository,
            ISettleTransactionRepository transactionRepository,
            IGroupInviteRepository groupInviteRepository)
        {
            _groupRepository = groupRepository;
            _userRepository = userRepository;
            _expenseRepository = expenseRepository;
            _transactionRepository = transactionRepository;
            _groupInviteRepository = groupInviteRepository;
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

            return await MapToDetailDtoWithStatsAsync(group, creatorUserId);
        }

        public async Task<GroupDetailResponseDto> GetGroupDetailAsync(string groupId, string? currentUserId = null)
        {
            var group = await _groupRepository.GetByIdWithMembersAsync(groupId);
            if (group == null)
            {
                throw new KeyNotFoundException("Không tìm thấy nhóm chi tiêu.");
            }

            return await MapToDetailDtoWithStatsAsync(group, currentUserId);
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
                    result.Add(await MapToDetailDtoWithStatsAsync(fullGroup, userId));
                }
            }

            return result;
        }

        public async Task<GroupMemberDto> AddVirtualMemberAsync(string groupId, string nickname, string requesterUserId)
        {
            var group = await _groupRepository.GetByIdWithMembersAsync(groupId);
            if (group == null)
            {
                throw new KeyNotFoundException("Không tìm thấy nhóm chi tiêu.");
            }

            if (!group.Members.Any(m => m.UserId == requesterUserId))
            {
                throw new UnauthorizedAccessException("Bạn không có quyền thao tác trên nhóm này.");
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

        public async Task<GroupMemberDto> LinkMemberAccountByEmailAsync(string groupId, string memberId, string email, string requesterUserId)
        {
            var group = await _groupRepository.GetByIdWithMembersAsync(groupId);
            if (group == null)
            {
                throw new KeyNotFoundException("Không tìm thấy nhóm chi tiêu.");
            }
            if (!group.Members.Any(m => m.UserId == requesterUserId))
            {
                throw new UnauthorizedAccessException("Bạn không có quyền thao tác trên nhóm này.");
            }

            var normalizedEmail = email?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedEmail))
            {
                throw new ArgumentException("Email liên kết không hợp lệ.");
            }

            var user = await _userRepository.GetByEmailAsync(normalizedEmail);
            if (user == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy tài khoản người dùng với email '{normalizedEmail}'.");
            }
            return await LinkMemberAccountAsync(groupId, memberId, user.Id);
        }

        public async Task<GroupInviteResponseDto> CreateInviteAsync(string groupId, string requesterUserId, CreateGroupInviteRequestDto? request = null)
        {
            var group = await _groupRepository.GetByIdWithMembersAsync(groupId);
            if (group == null)
            {
                throw new KeyNotFoundException("Không tìm thấy nhóm chi tiêu.");
            }

            EnsureCanManageInvite(group, requesterUserId);

            var expiresInHours = request?.ExpiresInHours ?? 72;
            if (expiresInHours < 1 || expiresInHours > 24 * 30)
            {
                throw new ArgumentException("Thời hạn lời mời phải nằm trong khoảng từ 1 giờ đến 720 giờ.");
            }

            var maxUses = request?.MaxUses ?? 10;
            if (maxUses < 1 || maxUses > 200)
            {
                throw new ArgumentException("Số lượt sử dụng tối đa phải nằm trong khoảng từ 1 đến 200.");
            }

            var invite = new GroupInvite
            {
                GroupId = groupId,
                CreatedByUserId = requesterUserId,
                Token = GenerateInviteToken(),
                ExpiresAt = DateTime.UtcNow.AddHours(expiresInHours),
                MaxUses = maxUses
            };

            await _groupInviteRepository.AddAsync(invite);

            return MapInviteToDto(invite, group.Name);
        }

        public async Task<IEnumerable<GroupInviteResponseDto>> GetInvitesAsync(string groupId, string requesterUserId)
        {
            var group = await _groupRepository.GetByIdWithMembersAsync(groupId);
            if (group == null)
            {
                throw new KeyNotFoundException("Không tìm thấy nhóm chi tiêu.");
            }

            EnsureCanManageInvite(group, requesterUserId);

            var invites = await _groupInviteRepository.GetByGroupIdAsync(groupId);
            return invites.Select(i => MapInviteToDto(i, group.Name));
        }

        public async Task<GroupInviteResponseDto> RevokeInviteAsync(string groupId, string inviteToken, string requesterUserId)
        {
            var group = await _groupRepository.GetByIdWithMembersAsync(groupId);
            if (group == null)
            {
                throw new KeyNotFoundException("Không tìm thấy nhóm chi tiêu.");
            }

            EnsureCanManageInvite(group, requesterUserId);

            var normalizedToken = inviteToken?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedToken))
            {
                throw new ArgumentException("Mã lời mời không hợp lệ.");
            }

            var invite = await _groupInviteRepository.GetByTokenAsync(normalizedToken);
            if (invite == null || invite.GroupId != groupId)
            {
                throw new KeyNotFoundException("Không tìm thấy lời mời trong nhóm.");
            }

            if (!invite.IsRevoked)
            {
                invite.IsRevoked = true;
                await _groupInviteRepository.UpdateAsync(invite);
            }

            return MapInviteToDto(invite, group.Name);
        }

        public async Task<GroupDetailResponseDto> AcceptInviteAsync(string inviteToken, string userId)
        {
            var normalizedToken = inviteToken?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedToken))
            {
                throw new ArgumentException("Mã lời mời không hợp lệ.");
            }

            var invite = await _groupInviteRepository.GetByTokenAsync(normalizedToken);
            if (invite == null || invite.IsRevoked)
            {
                throw new KeyNotFoundException("Lời mời không tồn tại hoặc đã bị thu hồi.");
            }

            if (invite.ExpiresAt <= DateTime.UtcNow)
            {
                throw new InvalidOperationException("Lời mời đã hết hạn.");
            }

            if (invite.UsedCount >= invite.MaxUses)
            {
                throw new InvalidOperationException("Lời mời đã đạt số lượt sử dụng tối đa.");
            }

            var group = await _groupRepository.GetByIdWithMembersAsync(invite.GroupId);
            if (group == null)
            {
                throw new KeyNotFoundException("Nhóm được mời không còn tồn tại.");
            }

            var existingMember = group.Members.FirstOrDefault(m => m.UserId == userId);
            var joinedAsNewMember = false;
            if (existingMember == null)
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    throw new KeyNotFoundException("Không tìm thấy tài khoản người dùng.");
                }

                var newMember = new GroupMember
                {
                    GroupId = group.Id,
                    UserId = user.Id,
                    Nickname = user.DisplayName,
                    IsVirtual = false
                };
                await _groupRepository.AddMemberAsync(newMember);
                joinedAsNewMember = true;
            }

            if (joinedAsNewMember)
            {
                invite.UsedCount += 1;
                await _groupInviteRepository.UpdateAsync(invite);
            }

            return await GetGroupDetailAsync(group.Id, userId);
        }

        public async Task RemoveMemberAsync(string groupId, string memberId, string requesterUserId)
        {
            var group = await _groupRepository.GetByIdWithMembersAsync(groupId);
            if (group == null)
            {
                throw new KeyNotFoundException("Không tìm thấy nhóm chi tiêu.");
            }
            if (group.CreatedById != requesterUserId)
            {
                throw new UnauthorizedAccessException("Chỉ trưởng nhóm mới có quyền xóa thành viên.");
            }

            var member = await _groupRepository.GetMemberAsync(groupId, memberId);
            if (member == null)
            {
                throw new KeyNotFoundException("Không tìm thấy thành viên trong nhóm.");
            }

            // TODO: Kiểm tra xem thành viên này có hóa đơn chưa thanh toán hoặc nợ chưa tất toán không
            // Để đơn giản, ta cho phép xóa, nhưng trong thực tế nên kiểm tra số dư nợ NetBalance = 0 mới được xóa.
            await _groupRepository.RemoveMemberAsync(member);
        }

        private async Task<GroupDetailResponseDto> MapToDetailDtoWithStatsAsync(Group group, string? currentUserId = null)
        {
            var dto = new GroupDetailResponseDto
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

            try
            {
                var expenses = await _expenseRepository.GetExpensesByGroupIdAsync(group.Id);
                dto.TotalSpent = expenses.Sum(e => e.TotalAmount);

                if (!string.IsNullOrEmpty(currentUserId))
                {
                    var userMember = group.Members.FirstOrDefault(m => m.UserId == currentUserId);
                    if (userMember != null)
                    {
                        var transactions = await _transactionRepository.GetTransactionsByGroupIdAsync(group.Id);
                        var completedTransactions = transactions.Where(t => t.PaymentStatus == "Completed").ToList();

                        // 1. Tính tổng tiền đã trả trong các hóa đơn
                        decimal paidInExpenses = expenses
                            .SelectMany(e => e.Payers)
                            .Where(p => p.MemberId == userMember.Id)
                            .Sum(p => p.AmountPaid);

                        // 2. Tính tổng tiền nợ phải chịu trong các hóa đơn
                        decimal owedInExpenses = expenses
                            .SelectMany(e => e.Slices)
                            .Where(s => s.MemberId == userMember.Id)
                            .Sum(s => s.AmountOwed);

                        // 3. Tính tổng tiền đã trả nợ trực tiếp (đã đối soát thành công)
                        decimal settledPaid = completedTransactions
                            .Where(t => t.DebtorId == userMember.Id)
                            .Sum(t => t.Amount);

                        // 4. Tính tổng tiền đã nhận thanh toán nợ trực tiếp
                        decimal settledReceived = completedTransactions
                            .Where(t => t.CreditorId == userMember.Id)
                            .Sum(t => t.Amount);

                        dto.UserNetBalance = (paidInExpenses - owedInExpenses) + (settledPaid - settledReceived);
                        dto.UserNetBalance = Math.Round(dto.UserNetBalance, 2);
                    }
                }
            }
            catch
            {
                dto.TotalSpent = 0;
                dto.UserNetBalance = 0;
            }

            return dto;
        }

        private static string GenerateInviteToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(24);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }

        private static void EnsureCanManageInvite(Group group, string requesterUserId)
        {
            var canManageInvite = group.CreatedById == requesterUserId || group.Members.Any(m => m.UserId == requesterUserId);
            if (!canManageInvite)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền quản lý lời mời cho nhóm này.");
            }
        }

        private static GroupInviteResponseDto MapInviteToDto(GroupInvite invite, string groupName)
        {
            return new GroupInviteResponseDto
            {
                InviteToken = invite.Token,
                GroupId = invite.GroupId,
                GroupName = groupName,
                MaxUses = invite.MaxUses,
                UsedCount = invite.UsedCount,
                IsRevoked = invite.IsRevoked,
                CreatedAt = invite.CreatedAt,
                ExpiresAt = invite.ExpiresAt
            };
        }
    }
}
