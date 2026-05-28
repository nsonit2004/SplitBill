using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SB_BusinessObjects;
using SB_BusinessObjects.Entities;
using SB_Repositories.Interfaces;

namespace SB_Repositories.Implementations
{
    public class SettleTransactionRepository : ISettleTransactionRepository
    {
        private readonly AppDbContext _context;

        public SettleTransactionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<SettleTransaction?> GetByIdAsync(string id)
        {
            return await _context.SettleTransactions
                .Include(t => t.Debtor)
                .Include(t => t.Creditor)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<IEnumerable<SettleTransaction>> GetTransactionsByGroupIdAsync(string groupId)
        {
            return await _context.SettleTransactions
                .Where(t => t.GroupId == groupId)
                .Include(t => t.Debtor)
                .Include(t => t.Creditor)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task AddAsync(SettleTransaction transaction)
        {
            await _context.SettleTransactions.AddAsync(transaction);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(SettleTransaction transaction)
        {
            _context.SettleTransactions.Update(transaction);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(SettleTransaction transaction)
        {
            _context.SettleTransactions.Remove(transaction);
            await _context.SaveChangesAsync();
        }
    }
}
