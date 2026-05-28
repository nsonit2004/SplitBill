using System.Threading.Tasks;
using SB_BusinessObjects.Entities;

namespace SB_Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(string id);
        Task<User?> GetByEmailAsync(string email);
        Task AddAsync(User user);
        Task UpdateAsync(User user);
        Task DeleteAsync(User user);
        Task<bool> ExistsByEmailAsync(string email);
    }
}
