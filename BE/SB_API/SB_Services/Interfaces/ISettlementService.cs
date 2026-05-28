using System.Collections.Generic;
using System.Threading.Tasks;
using SB_Services.DTOs;

namespace SB_Services.Interfaces
{
    public interface ISettlementService
    {
        Task<IEnumerable<MemberBalanceDto>> GetGroupBalancesAsync(string groupId);
        Task<IEnumerable<SettleTransactionResponseDto>> GetGroupSimplifiedDebtsAsync(string groupId);
        Task<SettleTransactionResponseDto> CreateSettleTransactionAsync(string groupId, CreateSettleTransactionRequestDto request);
        Task<SettleTransactionResponseDto> CompleteSettleTransactionAsync(string transactionId);
        Task<SettleTransactionResponseDto> UploadProofImageAsync(string transactionId, string proofImageUrl);
        Task<IEnumerable<SettleTransactionResponseDto>> GetGroupTransactionsHistoryAsync(string groupId);
    }
}
