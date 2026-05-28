using System.IO;
using System.Threading.Tasks;

namespace SB_Services.Interfaces
{
    public interface ICloudinaryService
    {
        Task<(string? ImageUrl, string? ErrorMessage)> UploadImageAsync(string base64Image, string folderName);
        Task<(string? ImageUrl, string? ErrorMessage)> UploadImageStreamAsync(Stream fileStream, string fileName, string folderName);
    }
}
