using System.Collections.Generic;
using System.Threading.Tasks;
using SB_BusinessObjects.Entities;

namespace SB_Repositories.Interfaces
{
    public interface ISettleTransactionRepository
    {
        Task<SettleTransaction?> GetByIdAsync(string id);
        Task<SettleTransaction?> GetByTransferReferenceAsync(string transferReference);
        Task<SettleTransaction?> GetLatestPendingBySignatureAsync(string groupId, string debtorId, string creditorId, decimal amount, string paymentMethod);
        Task<IEnumerable<SettleTransaction>> GetPendingBySignatureAsync(
            string groupId,
            string debtorId,
            string creditorId,
            decimal amount,
            string paymentMethod,
            string? excludeTransactionId = null);
        Task<IEnumerable<SettleTransaction>> GetTransactionsByGroupIdAsync(string groupId);
        Task AddAsync(SettleTransaction transaction);
        Task UpdateAsync(SettleTransaction transaction);
        Task DeleteAsync(SettleTransaction transaction);
    }
}
