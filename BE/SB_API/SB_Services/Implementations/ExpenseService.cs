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

        public ExpenseService(
            IExpenseRepository expenseRepository,
            IGroupRepository groupRepository,
            SplitStrategyFactory splitStrategyFactory)
        {
            _expenseRepository = expenseRepository;
            _groupRepository = groupRepository;
            _splitStrategyFactory = splitStrategyFactory;
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

            return MapToDto(expense, group.Members);
        }

        public async Task<IEnumerable<ExpenseResponseDto>> GetGroupExpensesAsync(string groupId)
        {
            var group = await _groupRepository.GetByIdWithMembersAsync(groupId);
            if (group == null)
            {
                throw new KeyNotFoundException("Không tìm thấy nhóm chi tiêu.");
            }

            var expenses = await _expenseRepository.GetExpensesByGroupIdAsync(groupId);
            return expenses.Select(e => MapToDto(e, group.Members)).ToList();
        }

        public async Task<ExpenseResponseDto> GetExpenseDetailAsync(string expenseId)
        {
            var expense = await _expenseRepository.GetByIdWithDetailsAsync(expenseId);
            if (expense == null)
            {
                throw new KeyNotFoundException("Không tìm thấy hóa đơn chi tiêu.");
            }

            var group = await _groupRepository.GetByIdWithMembersAsync(expense.GroupId);
            var members = group?.Members ?? new List<GroupMember>();

            return MapToDto(expense, members);
        }

        public async Task DeleteExpenseAsync(string expenseId)
        {
            var expense = await _expenseRepository.GetByIdAsync(expenseId);
            if (expense == null)
            {
                throw new KeyNotFoundException("Không tìm thấy hóa đơn chi tiêu.");
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
