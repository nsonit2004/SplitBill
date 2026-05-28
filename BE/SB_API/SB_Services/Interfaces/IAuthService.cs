using System.Threading.Tasks;
using SB_BusinessObjects.Entities;
using SB_Services.DTOs;

namespace SB_Services.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request);
        Task<AuthResponseDto> LoginAsync(LoginRequestDto request);
        Task<User?> GetCurrentUserAsync(string userId);
    }
}
