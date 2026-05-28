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
        private readonly IEmailService _emailService;

        public SettlementService(
            IGroupRepository groupRepository,
            IExpenseRepository expenseRepository,
            ISettleTransactionRepository transactionRepository,
            IUserRepository userRepository,
            IEmailService emailService)
        {
            _groupRepository = groupRepository;
            _expenseRepository = expenseRepository;
            _transactionRepository = transactionRepository;
            _userRepository = userRepository;
            _emailService = emailService;
        }

        public async Task<IEnumerable<MemberBalanceDto>> GetGroupBalancesAsync(string groupId, string requesterUserId)
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

        public async Task<IEnumerable<SettleTransactionResponseDto>> GetGroupSimplifiedDebtsAsync(string groupId, string requesterUserId)
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

            // Tính số dư Net Balance hiện tại của từng thành viên
            var balances = await GetGroupBalancesAsync(groupId, requesterUserId);
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
                string? bankCode = null;
                string? bankAccountNo = null;
                string? bankAccountName = null;

                // Nếu chủ nợ là user thật đã cấu hình thông tin ngân hàng thì tự động sinh link VietQR
                if (!creditor.IsVirtual && creditor.UserId != null)
                {
                    var creditorUser = await _userRepository.GetByIdAsync(creditor.UserId);
                    if (creditorUser != null && !string.IsNullOrEmpty(creditorUser.BankCode) && !string.IsNullOrEmpty(creditorUser.BankAccountNo))
                    {
                        bankCode = creditorUser.BankCode;
                        bankAccountNo = creditorUser.BankAccountNo;
                        bankAccountName = creditorUser.BankAccountName ?? creditorUser.DisplayName;

                        string cleanName = Uri.EscapeDataString(bankAccountName);
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
                    VietQrUrl = vietQrUrl,
                    BankCode = bankCode,
                    BankAccountNo = bankAccountNo,
                    BankAccountName = bankAccountName
                });
            }

            return result;
        }

        public async Task<SettleTransactionResponseDto> CreateSettleTransactionAsync(string groupId, CreateSettleTransactionRequestDto request, string requesterUserId)
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

            if (request.Amount <= 0)
            {
                throw new ArgumentException("Số tiền thanh toán không hợp lệ.");
            }

            if (request.DebtorId == request.CreditorId)
            {
                throw new ArgumentException("Người trả và người nhận không được trùng nhau.");
            }

            if (debtor.UserId != requesterUserId)
            {
                throw new UnauthorizedAccessException("Bạn chỉ có thể tạo thanh toán cho khoản nợ của chính mình.");
            }

            var normalizedPaymentMethod = (request.PaymentMethod ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedPaymentMethod))
            {
                throw new ArgumentException("Phương thức thanh toán không hợp lệ.");
            }
            var isCashPayment = string.Equals(normalizedPaymentMethod, "Cash", StringComparison.OrdinalIgnoreCase);

            // Tránh tạo trùng transaction Pending khi user mở modal nhiều lần cho cùng một khoản nợ
            if (!isCashPayment)
            {
                var existingPending = await _transactionRepository.GetLatestPendingBySignatureAsync(
                    groupId,
                    request.DebtorId,
                    request.CreditorId,
                    request.Amount,
                    normalizedPaymentMethod);
                if (existingPending != null)
                {
                    return await MapToResponseDtoAsync(existingPending, group.Members);
                }
            }

            var transaction = new SettleTransaction
            {
                GroupId = groupId,
                DebtorId = request.DebtorId,
                CreditorId = request.CreditorId,
                Amount = request.Amount,
                PaymentMethod = normalizedPaymentMethod,
                PaymentStatus = isCashPayment ? "Completed" : "Pending",
                TransferReference = isCashPayment ? null : $"SB_SETTLE_{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}"
            };

            await _transactionRepository.AddAsync(transaction);

            return await MapToResponseDtoAsync(transaction, group.Members);
        }

        public async Task<SettleTransactionResponseDto> CancelSettleTransactionAsync(string transactionId, string requesterUserId)
        {
            var transaction = await _transactionRepository.GetByIdAsync(transactionId);
            if (transaction == null)
            {
                throw new KeyNotFoundException("Không tìm thấy giao dịch đối soát nợ.");
            }

            if (transaction.PaymentStatus == "Completed")
            {
                throw new ArgumentException("Giao dịch đã hoàn tất, không thể hủy.");
            }

            var group = await _groupRepository.GetByIdWithMembersAsync(transaction.GroupId);
            var debtorMember = group?.Members.FirstOrDefault(m => m.Id == transaction.DebtorId);
            if (debtorMember == null)
            {
                throw new KeyNotFoundException("Không tìm thấy người trả của giao dịch.");
            }

            if (debtorMember.UserId != requesterUserId)
            {
                throw new UnauthorizedAccessException("Chỉ người trả tiền mới có thể hủy giao dịch.");
            }

            transaction.PaymentStatus = "Cancelled";
            transaction.UpdatedAt = DateTime.UtcNow;
            await _transactionRepository.UpdateAsync(transaction);

            return await MapToResponseDtoAsync(transaction, group?.Members ?? new List<GroupMember>());
        }

        public async Task<SettleTransactionResponseDto> CompleteSettleTransactionAsync(string transactionId, string requesterUserId)
        {
            var transaction = await _transactionRepository.GetByIdAsync(transactionId);
            if (transaction == null)
            {
                throw new KeyNotFoundException("Không tìm thấy giao dịch đối soát nợ.");
            }

            var group = await _groupRepository.GetByIdWithMembersAsync(transaction.GroupId);
            var creditorMember = group?.Members.FirstOrDefault(m => m.Id == transaction.CreditorId);
            if (creditorMember == null)
            {
                throw new KeyNotFoundException("Không tìm thấy chủ nợ của giao dịch.");
            }

            if (creditorMember.UserId != requesterUserId)
            {
                throw new UnauthorizedAccessException("Chỉ người nhận tiền mới có thể duyệt hoàn tất giao dịch.");
            }

            transaction.PaymentStatus = "Completed";
            transaction.UpdatedAt = DateTime.UtcNow;

            await _transactionRepository.UpdateAsync(transaction);
            await CancelDuplicatePendingTransactionsAsync(transaction);

            var members = group?.Members ?? new List<GroupMember>();

            return await MapToResponseDtoAsync(transaction, members);
        }

        public async Task<SettleTransactionResponseDto> CompleteByWebhookAsync(BankTransferWebhookDto webhookPayload)
        {
            var transferReference = (webhookPayload.TransferReference ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(transferReference))
            {
                throw new ArgumentException("Webhook thiếu TransferReference.");
            }

            var transaction = await _transactionRepository.GetByTransferReferenceAsync(transferReference);
            if (transaction == null)
            {
                throw new KeyNotFoundException("Không tìm thấy giao dịch phù hợp với TransferReference.");
            }

            if (transaction.PaymentStatus == "Completed")
            {
                var existedGroup = await _groupRepository.GetByIdWithMembersAsync(transaction.GroupId);
                return await MapToResponseDtoAsync(transaction, existedGroup?.Members ?? new List<GroupMember>());
            }

            if (transaction.Amount != webhookPayload.Amount)
            {
                throw new ArgumentException("Số tiền webhook không khớp giao dịch.");
            }

            transaction.PaymentStatus = "Completed";
            transaction.BankVerifiedAt = webhookPayload.PaidAtUtc ?? DateTime.UtcNow;
            transaction.UpdatedAt = DateTime.UtcNow;
            await _transactionRepository.UpdateAsync(transaction);
            await CancelDuplicatePendingTransactionsAsync(transaction);

            var group = await _groupRepository.GetByIdWithMembersAsync(transaction.GroupId);
            return await MapToResponseDtoAsync(transaction, group?.Members ?? new List<GroupMember>());
        }

        public async Task<SettleTransactionResponseDto> UploadProofImageAsync(string transactionId, string proofImageUrl, string requesterUserId)
        {
            var transaction = await _transactionRepository.GetByIdAsync(transactionId);
            if (transaction == null)
            {
                throw new KeyNotFoundException("Không tìm thấy giao dịch đối soát nợ.");
            }

            var group = await _groupRepository.GetByIdWithMembersAsync(transaction.GroupId);
            var debtorMember = group?.Members.FirstOrDefault(m => m.Id == transaction.DebtorId);
            if (debtorMember == null)
            {
                throw new KeyNotFoundException("Không tìm thấy người trả của giao dịch.");
            }

            if (debtorMember.UserId != requesterUserId)
            {
                throw new UnauthorizedAccessException("Chỉ người trả tiền mới có thể cập nhật minh chứng giao dịch.");
            }

            if (string.IsNullOrWhiteSpace(proofImageUrl) ||
                !Uri.TryCreate(proofImageUrl, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            {
                throw new ArgumentException("Đường dẫn minh chứng không hợp lệ.");
            }

            transaction.ProofImageUrl = proofImageUrl;
            transaction.UpdatedAt = DateTime.UtcNow;

            await _transactionRepository.UpdateAsync(transaction);

            var members = group?.Members ?? new List<GroupMember>();

            return await MapToResponseDtoAsync(transaction, members);
        }

        public async Task<IEnumerable<SettleTransactionResponseDto>> GetGroupTransactionsHistoryAsync(string groupId, string requesterUserId)
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

            var transactions = await _transactionRepository.GetTransactionsByGroupIdAsync(groupId);
            var completedSignatures = transactions
                .Where(t => string.Equals(t.PaymentStatus, "Completed", StringComparison.OrdinalIgnoreCase))
                .Select(t => $"{t.GroupId}|{t.DebtorId}|{t.CreditorId}|{t.Amount}|{(t.PaymentMethod ?? string.Empty).Trim().ToUpperInvariant()}")
                .ToHashSet();

            var normalizedTransactions = transactions
                .Where(t =>
                {
                    var status = (t.PaymentStatus ?? string.Empty).Trim().ToUpperInvariant();
                    if (status != "PENDING")
                    {
                        return true;
                    }

                    var pendingSignature = $"{t.GroupId}|{t.DebtorId}|{t.CreditorId}|{t.Amount}|{(t.PaymentMethod ?? string.Empty).Trim().ToUpperInvariant()}";
                    // Nếu đã có bản Completed cùng signature thì ẩn pending cũ để tránh cảm giác "tự sinh thêm"
                    return !completedSignatures.Contains(pendingSignature);
                })
                .GroupBy(t =>
                {
                    var status = (t.PaymentStatus ?? string.Empty).Trim().ToUpperInvariant();
                    if (status == "PENDING")
                    {
                        return $"{t.GroupId}|{t.DebtorId}|{t.CreditorId}|{t.Amount}|{(t.PaymentMethod ?? string.Empty).Trim().ToUpperInvariant()}|PENDING";
                    }

                    // Không gộp với trạng thái Completed/Cancelled để giữ đầy đủ lịch sử thật
                    return t.Id;
                })
                .Select(g => g.OrderByDescending(x => x.CreatedAt).First())
                .OrderByDescending(t => t.CreatedAt)
                .ToList();
            var result = new List<SettleTransactionResponseDto>();

            foreach (var transaction in normalizedTransactions)
            {
                result.Add(await MapToResponseDtoAsync(transaction, group.Members));
            }

            return result;
        }

        private async Task CancelDuplicatePendingTransactionsAsync(SettleTransaction completedTransaction)
        {
            var duplicatePendings = await _transactionRepository.GetPendingBySignatureAsync(
                completedTransaction.GroupId,
                completedTransaction.DebtorId,
                completedTransaction.CreditorId,
                completedTransaction.Amount,
                completedTransaction.PaymentMethod,
                completedTransaction.Id);

            var utcNow = DateTime.UtcNow;
            foreach (var pending in duplicatePendings)
            {
                pending.PaymentStatus = "Cancelled";
                pending.UpdatedAt = utcNow;
                await _transactionRepository.UpdateAsync(pending);
            }
        }

        private async Task<SettleTransactionResponseDto> MapToResponseDtoAsync(SettleTransaction transaction, ICollection<GroupMember> members)
        {
            var membersDict = members.ToDictionary(m => m.Id, m => m.Nickname);
            var creditor = members.FirstOrDefault(m => m.Id == transaction.CreditorId);

            string? vietQrUrl = null;
            string? bankCode = null;
            string? bankAccountNo = null;
            string? bankAccountName = null;

            if (creditor != null && !creditor.IsVirtual && creditor.UserId != null)
            {
                var creditorUser = await _userRepository.GetByIdAsync(creditor.UserId);
                if (creditorUser != null && !string.IsNullOrEmpty(creditorUser.BankCode) && !string.IsNullOrEmpty(creditorUser.BankAccountNo))
                {
                    bankCode = creditorUser.BankCode;
                    bankAccountNo = creditorUser.BankAccountNo;
                    bankAccountName = creditorUser.BankAccountName ?? creditorUser.DisplayName;

                    string cleanName = Uri.EscapeDataString(bankAccountName);
                    var transferRef = transaction.TransferReference ?? $"SB_SETTLE_{transaction.Id.Substring(0, 8)}";
                    string addInfo = Uri.EscapeDataString(transferRef);
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
                TransferReference = transaction.TransferReference,
                ProofImageUrl = transaction.ProofImageUrl,
                BankVerifiedAt = transaction.BankVerifiedAt,
                CreatedAt = transaction.CreatedAt,
                UpdatedAt = transaction.UpdatedAt,
                VietQrUrl = vietQrUrl,
                BankCode = bankCode,
                BankAccountNo = bankAccountNo,
                BankAccountName = bankAccountName
            };
        }

        public async Task NudgeDebtorAsync(string groupId, string debtorId, string creditorId, decimal amount, string requesterUserId)
        {
            var group = await _groupRepository.GetByIdWithMembersAsync(groupId);
            if (group == null)
            {
                throw new KeyNotFoundException("Không tìm thấy nhóm chi tiêu.");
            }

            var creditorMember = group.Members.FirstOrDefault(m => m.Id == creditorId);
            var debtorMember = group.Members.FirstOrDefault(m => m.Id == debtorId);

            if (creditorMember == null || debtorMember == null)
            {
                throw new ArgumentException("Thành viên không thuộc nhóm này.");
            }

            if (creditorMember.UserId != requesterUserId)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền thực hiện nhắc nợ này.");
            }

            if (debtorMember.IsVirtual || string.IsNullOrEmpty(debtorMember.UserId))
            {
                throw new ArgumentException("Thành viên này là tài khoản ảo (Guest) nên không có địa chỉ email để gửi nhắc nợ.");
            }

            var debtorUser = await _userRepository.GetByIdAsync(debtorMember.UserId);
            if (debtorUser == null || string.IsNullOrEmpty(debtorUser.Email))
            {
                throw new ArgumentException("Không tìm thấy tài khoản người dùng tương ứng với thành viên nợ.");
            }

            string subject = $"[SplitBill Pro] Nhắc thanh toán khoản nợ trong nhóm '{group.Name}'";
            string body = $@"
                <div style='font-family: sans-serif; padding: 20px; max-width: 600px; border: 1px solid #e2e8f0; border-radius: 12px; background-color: #ffffff;'>
                    <h2 style='color: #4f46e5; margin-bottom: 20px;'>Chào {debtorMember.Nickname},</h2>
                    <p style='color: #334155; font-size: 14px; line-height: 1.6;'>
                        Bạn nhận được email này vì thành viên <strong>{creditorMember.Nickname}</strong> đã gửi nhắc nợ cho bạn trong nhóm chi tiêu <strong>{group.Name}</strong>.
                    </p>
                    <div style='margin: 20px 0; padding: 15px; border-radius: 8px; background-color: #f8fafc; border-left: 4px solid #ef4444;'>
                        <p style='margin: 0; font-size: 13px; color: #64748b;'>Khoản nợ cần thanh toán:</p>
                        <p style='margin: 5px 0 0 0; font-size: 20px; font-weight: bold; color: #b91c1c;'>{amount:N0} VND</p>
                    </div>
                    <p style='color: #334155; font-size: 14px; line-height: 1.6;'>
                        Vui lòng truy cập ứng dụng <strong>SplitBill Pro</strong>, quét mã VietQR để thực hiện thanh toán nhanh và tất toán khoản nợ này.
                    </p>
                    <hr style='border: 0; border-top: 1px solid #e2e8f0; margin: 20px 0;' />
                    <p style='font-size: 11px; color: #94a3b8; text-align: center; margin: 0;'>
                        Đây là email tự động gửi từ hệ thống SplitBill Pro. Vui lòng không trả lời thư này.
                    </p>
                </div>";

            await _emailService.SendEmailAsync(debtorUser.Email, subject, body);
        }
    }
}
