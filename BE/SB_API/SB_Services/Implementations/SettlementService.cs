using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SB_BusinessObjects.Entities;
using SB_Repositories.Interfaces;
using SB_Services.DTOs;
using SB_Services.Helpers;
using SB_Services.Interfaces;

namespace SB_Services.Implementations
{
    public class SettlementService : ISettlementService
    {
        private readonly IGroupRepository _groupRepository;
        private readonly IExpenseRepository _expenseRepository;
        private readonly ISettleTransactionRepository _transactionRepository;
        private readonly IUserRepository _userRepository;

        public SettlementService(
            IGroupRepository groupRepository,
            IExpenseRepository expenseRepository,
            ISettleTransactionRepository transactionRepository,
            IUserRepository userRepository)
        {
            _groupRepository = groupRepository;
            _expenseRepository = expenseRepository;
            _transactionRepository = transactionRepository;
            _userRepository = userRepository;
        }

        public async Task<IEnumerable<MemberBalanceDto>> GetGroupBalancesAsync(string groupId)
        {
            var group = await _groupRepository.GetByIdWithMembersAsync(groupId);
            if (group == null)
            {
                throw new KeyNotFoundException("Không tìm thấy nhóm chi tiêu.");
            }

            var expenses = await _expenseRepository.GetExpensesByGroupIdAsync(groupId);
            var transactions = await _transactionRepository.GetTransactionsByGroupIdAsync(groupId);
            var completedTransactions = transactions.Where(t => t.PaymentStatus == "Completed").ToList();

            var balances = new List<MemberBalanceDto>();

            foreach (var member in group.Members)
            {
                // 1. Tính tổng tiền đã trả trong các hóa đơn
                decimal paidInExpenses = expenses
                    .SelectMany(e => e.Payers)
                    .Where(p => p.MemberId == member.Id)
                    .Sum(p => p.AmountPaid);

                // 2. Tính tổng tiền nợ phải chịu trong các hóa đơn
                decimal owedInExpenses = expenses
                    .SelectMany(e => e.Slices)
                    .Where(s => s.MemberId == member.Id)
                    .Sum(s => s.AmountOwed);

                // 3. Tính tổng tiền đã trả nợ trực tiếp (đã đối soát thành công)
                decimal settledPaid = completedTransactions
                    .Where(t => t.DebtorId == member.Id)
                    .Sum(t => t.Amount);

                // 4. Tính tổng tiền đã nhận thanh toán nợ trực tiếp
                decimal settledReceived = completedTransactions
                    .Where(t => t.CreditorId == member.Id)
                    .Sum(t => t.Amount);

                // Số dư ròng hiện tại (NetBalance):
                // - Số dư dương: Bạn đang là chủ nợ, cần được nhận lại tiền
                // - Số dư âm: Bạn đang nợ nhóm, cần chuyển khoản trả nợ
                decimal netBalance = (paidInExpenses - owedInExpenses) + (settledPaid - settledReceived);
                netBalance = Math.Round(netBalance, 2);

                balances.Add(new MemberBalanceDto
                {
                    MemberId = member.Id,
                    Nickname = member.Nickname,
                    IsVirtual = member.IsVirtual,
                    PaidInExpenses = paidInExpenses,
                    OwedInExpenses = owedInExpenses,
                    SettledPaid = settledPaid,
                    SettledReceived = settledReceived,
                    NetBalance = netBalance
                });
            }

            return balances;
        }

        public async Task<IEnumerable<SettleTransactionResponseDto>> GetGroupSimplifiedDebtsAsync(string groupId)
        {
            var group = await _groupRepository.GetByIdWithMembersAsync(groupId);
            if (group == null)
            {
                throw new KeyNotFoundException("Không tìm thấy nhóm chi tiêu.");
            }

            // Tính số dư Net Balance hiện tại của từng thành viên
            var balances = await GetGroupBalancesAsync(groupId);
            var balanceDict = balances.ToDictionary(b => b.MemberId, b => b.NetBalance);

            // Chạy thuật toán rút gọn nợ để sinh danh sách thanh toán tối ưu nhất
            var simplifiedDebts = DebtSimplifier.Simplify(balanceDict);

            var membersDict = group.Members.ToDictionary(m => m.Id, m => m);
            var result = new List<SettleTransactionResponseDto>();

            foreach (var debt in simplifiedDebts)
            {
                var debtor = membersDict[debt.DebtorId];
                var creditor = membersDict[debt.CreditorId];

                string? vietQrUrl = null;
                // Nếu chủ nợ là user thật đã cấu hình thông tin ngân hàng thì tự động sinh link VietQR
                if (!creditor.IsVirtual && creditor.UserId != null)
                {
                    var creditorUser = await _userRepository.GetByIdAsync(creditor.UserId);
                    if (creditorUser != null && !string.IsNullOrEmpty(creditorUser.BankCode) && !string.IsNullOrEmpty(creditorUser.BankAccountNo))
                    {
                        string cleanName = Uri.EscapeDataString(creditorUser.BankAccountName ?? creditorUser.DisplayName);
                        string addInfo = Uri.EscapeDataString($"SPLITBILL {group.Name.ToUpper()}");
                        vietQrUrl = $"https://img.vietqr.io/image/{creditorUser.BankCode}-{creditorUser.BankAccountNo}-compact2.jpg?amount={(int)debt.Amount}&addInfo={addInfo}&accountName={cleanName}";
                    }
                }

                result.Add(new SettleTransactionResponseDto
                {
                    Id = string.Empty, // Chỉ là đề xuất thanh toán ảo, chưa lưu vào DB
                    GroupId = groupId,
                    DebtorId = debtor.Id,
                    DebtorNickname = debtor.Nickname,
                    CreditorId = creditor.Id,
                    CreditorNickname = creditor.Nickname,
                    Amount = debt.Amount,
                    PaymentMethod = "VietQR",
                    PaymentStatus = "Suggested",
                    VietQrUrl = vietQrUrl
                });
            }

            return result;
        }

        public async Task<SettleTransactionResponseDto> CreateSettleTransactionAsync(string groupId, CreateSettleTransactionRequestDto request)
        {
            var group = await _groupRepository.GetByIdWithMembersAsync(groupId);
            if (group == null)
            {
                throw new KeyNotFoundException("Không tìm thấy nhóm chi tiêu.");
            }

            var debtor = group.Members.FirstOrDefault(m => m.Id == request.DebtorId);
            var creditor = group.Members.FirstOrDefault(m => m.Id == request.CreditorId);

            if (debtor == null || creditor == null)
            {
                throw new ArgumentException("Người chuyển nợ hoặc người nhận nợ không thuộc nhóm này.");
            }

            var transaction = new SettleTransaction
            {
                GroupId = groupId,
                DebtorId = request.DebtorId,
                CreditorId = request.CreditorId,
                Amount = request.Amount,
                PaymentMethod = request.PaymentMethod,
                PaymentStatus = "Pending"
            };

            await _transactionRepository.AddAsync(transaction);

            return await MapToResponseDtoAsync(transaction, group.Members);
        }

        public async Task<SettleTransactionResponseDto> CompleteSettleTransactionAsync(string transactionId)
        {
            var transaction = await _transactionRepository.GetByIdAsync(transactionId);
            if (transaction == null)
            {
                throw new KeyNotFoundException("Không tìm thấy giao dịch đối soát nợ.");
            }

            transaction.PaymentStatus = "Completed";
            transaction.UpdatedAt = DateTime.UtcNow;

            await _transactionRepository.UpdateAsync(transaction);

            var group = await _groupRepository.GetByIdWithMembersAsync(transaction.GroupId);
            var members = group?.Members ?? new List<GroupMember>();

            return await MapToResponseDtoAsync(transaction, members);
        }

        public async Task<SettleTransactionResponseDto> UploadProofImageAsync(string transactionId, string proofImageUrl)
        {
            var transaction = await _transactionRepository.GetByIdAsync(transactionId);
            if (transaction == null)
            {
                throw new KeyNotFoundException("Không tìm thấy giao dịch đối soát nợ.");
            }

            transaction.ProofImageUrl = proofImageUrl;
            transaction.UpdatedAt = DateTime.UtcNow;

            await _transactionRepository.UpdateAsync(transaction);

            var group = await _groupRepository.GetByIdWithMembersAsync(transaction.GroupId);
            var members = group?.Members ?? new List<GroupMember>();

            return await MapToResponseDtoAsync(transaction, members);
        }

        public async Task<IEnumerable<SettleTransactionResponseDto>> GetGroupTransactionsHistoryAsync(string groupId)
        {
            var group = await _groupRepository.GetByIdWithMembersAsync(groupId);
            if (group == null)
            {
                throw new KeyNotFoundException("Không tìm thấy nhóm chi tiêu.");
            }

            var transactions = await _transactionRepository.GetTransactionsByGroupIdAsync(groupId);
            var result = new List<SettleTransactionResponseDto>();

            foreach (var transaction in transactions)
            {
                result.Add(await MapToResponseDtoAsync(transaction, group.Members));
            }

            return result;
        }

        private async Task<SettleTransactionResponseDto> MapToResponseDtoAsync(SettleTransaction transaction, ICollection<GroupMember> members)
        {
            var membersDict = members.ToDictionary(m => m.Id, m => m.Nickname);
            var creditor = members.FirstOrDefault(m => m.Id == transaction.CreditorId);

            string? vietQrUrl = null;
            if (creditor != null && !creditor.IsVirtual && creditor.UserId != null)
            {
                var creditorUser = await _userRepository.GetByIdAsync(creditor.UserId);
                if (creditorUser != null && !string.IsNullOrEmpty(creditorUser.BankCode) && !string.IsNullOrEmpty(creditorUser.BankAccountNo))
                {
                    string cleanName = Uri.EscapeDataString(creditorUser.BankAccountName ?? creditorUser.DisplayName);
                    string addInfo = Uri.EscapeDataString($"SB_SETTLE_{transaction.Id.Substring(0, 8)}");
                    vietQrUrl = $"https://img.vietqr.io/image/{creditorUser.BankCode}-{creditorUser.BankAccountNo}-compact2.jpg?amount={(int)transaction.Amount}&addInfo={addInfo}&accountName={cleanName}";
                }
            }

            return new SettleTransactionResponseDto
            {
                Id = transaction.Id,
                GroupId = transaction.GroupId,
                DebtorId = transaction.DebtorId,
                DebtorNickname = membersDict.TryGetValue(transaction.DebtorId, out var debtorName) ? debtorName : "Không rõ",
                CreditorId = transaction.CreditorId,
                CreditorNickname = membersDict.TryGetValue(transaction.CreditorId, out var creditorName) ? creditorName : "Không rõ",
                Amount = transaction.Amount,
                PaymentMethod = transaction.PaymentMethod,
                PaymentStatus = transaction.PaymentStatus,
                ProofImageUrl = transaction.ProofImageUrl,
                CreatedAt = transaction.CreatedAt,
                UpdatedAt = transaction.UpdatedAt,
                VietQrUrl = vietQrUrl
            };
        }
    }
}
