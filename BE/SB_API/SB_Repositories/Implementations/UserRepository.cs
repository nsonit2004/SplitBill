using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SB_BusinessObjects;
using SB_BusinessObjects.Entities;
using SB_Repositories.Interfaces;

namespace SB_Repositories.Implementations
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByIdAsync(string id)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            var normalizedEmail = (email ?? string.Empty).Trim().ToLower();
            if (string.IsNullOrEmpty(normalizedEmail))
            {
                return null;
            }

            return await _context.Users.FirstOrDefaultAsync(u =>
                u.Email != null && u.Email.Trim().ToLower() == normalizedEmail);
        }

        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(User user)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsByEmailAsync(string email)
        {
            var normalizedEmail = (email ?? string.Empty).Trim().ToLower();
            if (string.IsNullOrEmpty(normalizedEmail))
            {
                return false;
            }

            return await _context.Users.AnyAsync(u =>
                u.Email != null && u.Email.Trim().ToLower() == normalizedEmail);
        }
    }
}
