using System.Collections.Generic;
using System.Threading.Tasks;
using SB_BusinessObjects.Entities;

namespace SB_Repositories.Interfaces
{
    public interface ISettleTransactionRepository
    {
        Task<SettleTransaction?> GetByIdAsync(string id);
        Task<IEnumerable<SettleTransaction>> GetTransactionsByGroupIdAsync(string groupId);
        Task AddAsync(SettleTransaction transaction);
        Task UpdateAsync(SettleTransaction transaction);
        Task DeleteAsync(SettleTransaction transaction);
    }
}
