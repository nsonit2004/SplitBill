using System.Collections.Generic;
using System.Threading.Tasks;
using SB_BusinessObjects.Entities;

namespace SB_Repositories.Interfaces
{
    public interface IExpenseRepository
    {
        Task<Expense?> GetByIdAsync(string id);
        Task<Expense?> GetByIdWithDetailsAsync(string id);
        Task<IEnumerable<Expense>> GetExpensesByGroupIdAsync(string groupId);
        Task AddAsync(Expense expense);
        Task UpdateAsync(Expense expense);
        Task DeleteAsync(Expense expense);
    }
}
