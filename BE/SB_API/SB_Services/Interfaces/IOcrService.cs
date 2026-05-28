using System.IO;
using System.Threading.Tasks;
using SB_Services.DTOs;

namespace SB_Services.Interfaces
{
    public interface IOcrService
    {
        Task<(OcrScanResultDto? Result, string? ErrorMessage)> ScanReceiptAsync(Stream imageStream, string mimeType);
    }
}
