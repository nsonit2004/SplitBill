using System.Collections.Generic;
using System.Threading.Tasks;
using SB_Services.DTOs;

namespace SB_Services.Interfaces
{
    public interface IExpenseService
    {
        Task<ExpenseResponseDto> CreateExpenseAsync(string groupId, CreateExpenseRequestDto request, string creatorUserId);
        Task<IEnumerable<ExpenseResponseDto>> GetGroupExpensesAsync(string groupId, string requesterUserId);
        Task<ExpenseResponseDto> GetExpenseDetailAsync(string expenseId, string requesterUserId);
        Task DeleteExpenseAsync(string expenseId, string requesterUserId);
    }
}
