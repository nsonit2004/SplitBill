using System.IO;
using System.Threading.Tasks;

namespace SB_Services.Interfaces
{
    public interface ICloudinaryService
    {
        Task<string> UploadImageAsync(string base64Image, string folderName);
        Task<string> UploadImageStreamAsync(Stream fileStream, string fileName, string folderName);
    }
}
