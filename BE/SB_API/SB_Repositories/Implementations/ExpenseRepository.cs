using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SB_BusinessObjects;
using SB_BusinessObjects.Entities;
using SB_Repositories.Interfaces;

namespace SB_Repositories.Implementations
{
    public class ExpenseRepository : IExpenseRepository
    {
        private readonly AppDbContext _context;

        public ExpenseRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Expense?> GetByIdAsync(string id)
        {
            return await _context.Expenses.FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<Expense?> GetByIdWithDetailsAsync(string id)
        {
            return await _context.Expenses
                .Include(e => e.Payers)
                    .ThenInclude(p => p.Member)
                .Include(e => e.Slices)
                    .ThenInclude(s => s.Member)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<IEnumerable<Expense>> GetExpensesByGroupIdAsync(string groupId)
        {
            return await _context.Expenses
                .Where(e => e.GroupId == groupId)
                .Include(e => e.Payers)
                    .ThenInclude(p => p.Member)
                .Include(e => e.Slices)
                    .ThenInclude(s => s.Member)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }

        public async Task AddAsync(Expense expense)
        {
            await _context.Expenses.AddAsync(expense);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Expense expense)
        {
            // Do ExpensePayers và ExpenseSlices là các thực thể con có composite key,
            // EF Core sẽ tự xử lý việc cập nhật khi lưu thay đổi trên thực thể cha.
            _context.Expenses.Update(expense);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Expense expense)
        {
            _context.Expenses.Remove(expense);
            await _context.SaveChangesAsync();
        }
    }
}
