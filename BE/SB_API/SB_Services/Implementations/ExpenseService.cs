using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SB_BusinessObjects.Entities;
using SB_Repositories.Interfaces;
using SB_Services.DTOs;
using SB_Services.Interfaces;
using SB_Services.Strategies;

namespace SB_Services.Implementations
{
    public class ExpenseService : IExpenseService
    {
        private readonly IExpenseRepository _expenseRepository;
        private readonly IGroupRepository _groupRepository;
        private readonly SplitStrategyFactory _splitStrategyFactory;
        private readonly IEmailService _emailService;
        private readonly IUserRepository _userRepository;

        public ExpenseService(
            IExpenseRepository expenseRepository,
            IGroupRepository groupRepository,
            SplitStrategyFactory splitStrategyFactory,
            IEmailService emailService,
            IUserRepository userRepository)
        {
            _expenseRepository = expenseRepository;
            _groupRepository = groupRepository;
            _splitStrategyFactory = splitStrategyFactory;
            _emailService = emailService;
            _userRepository = userRepository;
        }

        public async Task<ExpenseResponseDto> CreateExpenseAsync(string groupId, CreateExpenseRequestDto request, string creatorUserId)
        {
            // 1. Kiểm tra nhóm tồn tại và load thông tin thành viên
            var group = await _groupRepository.GetByIdWithMembersAsync(groupId);
            if (group == null)
            {
                throw new KeyNotFoundException("Không tìm thấy nhóm chi tiêu.");
            }

            // 2. Xác thực và tính toán số tiền người trả (Payers)
            if (request.Payers == null || !request.Payers.Any())
            {
                throw new ArgumentException("Phải có ít nhất một người thanh toán hóa đơn.");
            }

            decimal totalPaid = request.Payers.Sum(p => p.AmountPaid);
            if (Math.Abs(totalPaid - request.TotalAmount) > 1.00M)
            {
                throw new ArgumentException("Tổng số tiền mọi người trả không khớp với tổng hóa đơn.");
            }

            // 3. Sử dụng Strategy Pattern để tính toán số tiền nợ (Slices) cho từng thành viên
            var strategy = _splitStrategyFactory.GetStrategy(request.SplitMethod);
            var groupMemberIds = group.Members.Select(m => m.Id).ToList();

            var splitValues = request.Slices.Select(s => new SplitValueDto
            {
                MemberId = s.MemberId,
                Value = s.Value
            }).ToList();

            // Nếu là chia đều (Equally) và không truyền thông tin slices, mặc định chia đều cho tất cả thành viên trong nhóm
            if (request.SplitMethod == "Equally" && (splitValues == null || !splitValues.Any()))
            {
                splitValues = group.Members.Select(m => new SplitValueDto { MemberId = m.Id, Value = 1 }).ToList();
            }

            var calculatedSlices = strategy.CalculateSplit(request.TotalAmount, groupMemberIds, splitValues);

            // 4. Tạo thực thể Expense
            var expense = new Expense
            {
                GroupId = groupId,
                Description = request.Description,
                TotalAmount = request.TotalAmount,
                SplitMethod = request.SplitMethod,
                ImageUrl = request.ImageUrl,
                Category = request.Category ?? "Other",
                CreatedById = creatorUserId
            };

            // Thêm thông tin người trả tiền (Payers)
            foreach (var payerDto in request.Payers)
            {
                if (!groupMemberIds.Contains(payerDto.MemberId))
                {
                    throw new ArgumentException($"Thành viên {payerDto.MemberId} không nằm trong nhóm này.");
                }

                expense.Payers.Add(new ExpensePayer
                {
                    ExpenseId = expense.Id,
                    MemberId = payerDto.MemberId,
                    AmountPaid = payerDto.AmountPaid
                });
            }

            // Thêm thông tin người chịu nợ (Slices)
            foreach (var slice in calculatedSlices)
            {
                if (slice.Value <= 0) continue; // Bỏ qua nếu không nợ đồng nào

                expense.Slices.Add(new ExpenseSlice
                {
                    ExpenseId = expense.Id,
                    MemberId = slice.Key,
                    AmountOwed = slice.Value
                });
            }

            await _expenseRepository.AddAsync(expense);

            // 5. Gửi email thông báo cho các thành viên nợ
            try
            {
                var creatorUser = await _userRepository.GetByIdAsync(creatorUserId);
                var creatorName = creatorUser?.DisplayName ?? "Một thành viên";

                foreach (var slice in calculatedSlices)
                {
                    if (slice.Value <= 0) continue; // không nợ đồng nào thì không gửi

                    var member = group.Members.FirstOrDefault(m => m.Id == slice.Key);
                    if (member == null || member.IsVirtual || string.IsNullOrEmpty(member.UserId))
                    {
                        continue; // Skip virtual members
                    }

                    // Không gửi cho chính người tạo/người nhập bill
                    if (member.UserId == creatorUserId) continue;

                    var debtorUser = await _userRepository.GetByIdAsync(member.UserId);
                    if (debtorUser != null && !string.IsNullOrEmpty(debtorUser.Email))
                    {
                        var emailTo = debtorUser.Email;
                        var nickname = member.Nickname;
                        var owedAmount = slice.Value;
                        var groupName = group.Name;
                        var expDesc = expense.Description;
                        var totalAmt = expense.TotalAmount;

                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                string subject = $"[SplitBill Pro] Hóa đơn mới '{expDesc}' trong nhóm '{groupName}'";
                                string body = $@"
                                    <div style='font-family: sans-serif; padding: 20px; max-width: 600px; border: 1px solid #e2e8f0; border-radius: 12px; background-color: #ffffff;'>
                                        <h2 style='color: #4f46e5; margin-bottom: 20px;'>Chào {nickname},</h2>
                                        <p style='color: #334155; font-size: 14px; line-height: 1.6;'>
                                            Thành viên <strong>{creatorName}</strong> vừa thêm một hóa đơn chi tiêu mới <strong>{expDesc}</strong> trong nhóm <strong>{groupName}</strong>.
                                        </p>
                                        <div style='margin: 20px 0; padding: 15px; border-radius: 8px; background-color: #f8fafc; border-left: 4px solid #4f46e5;'>
                                            <p style='margin: 0; font-size: 13px; color: #64748b;'>Tổng hóa đơn: {totalAmt:N0} VND</p>
                                            <p style='margin: 5px 0 0 0; font-size: 14px; color: #334155;'>Phần nợ bạn cần chịu:</p>
                                            <p style='margin: 5px 0 0 0; font-size: 20px; font-weight: bold; color: #4f46e5;'>{owedAmount:N0} VND</p>
                                        </div>
                                        <p style='color: #334155; font-size: 14px; line-height: 1.6;'>
                                            Vui lòng đăng nhập vào ứng dụng để xem chi tiết và tất toán nợ chi tiêu của mình.
                                        </p>
                                        <hr style='border: 0; border-top: 1px solid #e2e8f0; margin: 20px 0;' />
                                        <p style='font-size: 11px; color: #94a3b8; text-align: center; margin: 0;'>
                                            Đây là email tự động gửi từ hệ thống SplitBill Pro. Vui lòng không trả lời thư này.
                                        </p>
                                    </div>";
                                await _emailService.SendEmailAsync(emailTo, subject, body);
                            }
                            catch
                            {
                                // ignore errors in background task
                            }
                        });
                    }
                }
            }
            catch
            {
                // ignore main try/catch errors
            }

            return MapToDto(expense, group.Members);
        }

        public async Task<IEnumerable<ExpenseResponseDto>> GetGroupExpensesAsync(string groupId, string requesterUserId)
        {
            var group = await _groupRepository.GetByIdWithMembersAsync(groupId);
            if (group == null)
            {
                throw new KeyNotFoundException("Không tìm thấy nhóm chi tiêu.");
            }
            if (!group.Members.Any(m => m.UserId == requesterUserId))
            {
                throw new UnauthorizedAccessException("Bạn không có quyền truy cập dữ liệu nhóm này.");
            }

            var expenses = await _expenseRepository.GetExpensesByGroupIdAsync(groupId);
            return expenses.Select(e => MapToDto(e, group.Members)).ToList();
        }

        public async Task<ExpenseResponseDto> GetExpenseDetailAsync(string expenseId, string requesterUserId)
        {
            var expense = await _expenseRepository.GetByIdWithDetailsAsync(expenseId);
            if (expense == null)
            {
                throw new KeyNotFoundException("Không tìm thấy hóa đơn chi tiêu.");
            }

            var group = await _groupRepository.GetByIdWithMembersAsync(expense.GroupId);
            var members = group?.Members ?? new List<GroupMember>();
            if (group == null || !members.Any(m => m.UserId == requesterUserId))
            {
                throw new UnauthorizedAccessException("Bạn không có quyền truy cập dữ liệu nhóm này.");
            }

            return MapToDto(expense, members);
        }

        public async Task DeleteExpenseAsync(string expenseId, string requesterUserId)
        {
            var expense = await _expenseRepository.GetByIdAsync(expenseId);
            if (expense == null)
            {
                throw new KeyNotFoundException("Không tìm thấy hóa đơn chi tiêu.");
            }

            var group = await _groupRepository.GetByIdWithMembersAsync(expense.GroupId);
            if (group == null || !group.Members.Any(m => m.UserId == requesterUserId))
            {
                throw new UnauthorizedAccessException("Bạn không có quyền thao tác trên nhóm này.");
            }

            await _expenseRepository.DeleteAsync(expense);
        }

        private ExpenseResponseDto MapToDto(Expense expense, ICollection<GroupMember> members)
        {
            var memberDict = members.ToDictionary(m => m.Id, m => m.Nickname);

            return new ExpenseResponseDto
            {
                Id = expense.Id,
                GroupId = expense.GroupId,
                Description = expense.Description,
                TotalAmount = expense.TotalAmount,
                SplitMethod = expense.SplitMethod,
                ImageUrl = expense.ImageUrl,
                Category = expense.Category ?? "Other",
                CreatedById = expense.CreatedById,
                CreatedAt = expense.CreatedAt,
                Payers = expense.Payers.Select(p => new ExpensePayerDto
                {
                    MemberId = p.MemberId,
                    Nickname = memberDict.TryGetValue(p.MemberId, out var nickname) ? nickname : "Không rõ",
                    AmountPaid = p.AmountPaid
                }).ToList(),
                Slices = expense.Slices.Select(s => new ExpenseSliceDto
                {
                    MemberId = s.MemberId,
                    Nickname = memberDict.TryGetValue(s.MemberId, out var nickname) ? nickname : "Không rõ",
                    AmountOwed = s.AmountOwed
                }).ToList()
            };
        }
    }
}
