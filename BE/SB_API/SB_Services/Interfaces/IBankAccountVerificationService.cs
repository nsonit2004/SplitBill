using System.Threading.Tasks;
using SB_Services.DTOs;

namespace SB_Services.Interfaces
{
    public interface IBankAccountVerificationService
    {
        Task<BankAccountVerificationResultDto> VerifyAsync(string bankCode, string bankAccountNo, string bankAccountName);
    }
}
