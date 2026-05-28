using System.Collections.Generic;
using System.Threading.Tasks;
using SB_Services.DTOs;

namespace SB_Services.Interfaces
{
    public interface ISettlementService
    {
        Task<IEnumerable<MemberBalanceDto>> GetGroupBalancesAsync(string groupId, string requesterUserId);
        Task<IEnumerable<SettleTransactionResponseDto>> GetGroupSimplifiedDebtsAsync(string groupId, string requesterUserId);
        Task<SettleTransactionResponseDto> CreateSettleTransactionAsync(string groupId, CreateSettleTransactionRequestDto request, string requesterUserId);
        Task<SettleTransactionResponseDto> CancelSettleTransactionAsync(string transactionId, string requesterUserId);
        Task<SettleTransactionResponseDto> CompleteSettleTransactionAsync(string transactionId, string requesterUserId);
        Task<SettleTransactionResponseDto> UploadProofImageAsync(string transactionId, string proofImageUrl, string requesterUserId);
        Task<SettleTransactionResponseDto> CompleteByWebhookAsync(BankTransferWebhookDto webhookPayload);
        Task<IEnumerable<SettleTransactionResponseDto>> GetGroupTransactionsHistoryAsync(string groupId, string requesterUserId);
        Task NudgeDebtorAsync(string groupId, string debtorId, string creditorId, decimal amount, string requesterUserId);
    }
}
