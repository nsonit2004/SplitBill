using System;
using System.IO;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using SB_Services.Interfaces;

namespace SB_Services.Implementations
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary? _cloudinary;

        public CloudinaryService(IConfiguration configuration)
        {
            var cloudName = configuration["Cloudinary:CloudName"];
            var apiKey = configuration["Cloudinary:ApiKey"];
            var apiSecret = configuration["Cloudinary:ApiSecret"];

            if (!string.IsNullOrEmpty(cloudName) && !string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(apiSecret))
            {
                var account = new Account(cloudName, apiKey, apiSecret);
                _cloudinary = new Cloudinary(account);
            }
        }

        public async Task<string> UploadImageAsync(string base64Image, string folderName)
        {
            if (_cloudinary == null)
            {
                // Fallback giả lập khi chạy môi trường dev không cấu hình Cloudinary
                return "https://res.cloudinary.com/demo/image/upload/v1312461204/sample.jpg";
            }

            try
            {
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(Guid.NewGuid().ToString(), base64Image),
                    Folder = folderName
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                return uploadResult.SecureUrl?.ToString() ?? string.Empty;
            }
            catch (Exception)
            {
                return "https://res.cloudinary.com/demo/image/upload/v1312461204/sample.jpg";
            }
        }

        public async Task<string> UploadImageStreamAsync(Stream fileStream, string fileName, string folderName)
        {
            if (_cloudinary == null)
            {
                return "https://res.cloudinary.com/demo/image/upload/v1312461204/sample.jpg";
            }

            try
            {
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(fileName, fileStream),
                    Folder = folderName
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                return uploadResult.SecureUrl?.ToString() ?? string.Empty;
            }
            catch (Exception)
            {
                return "https://res.cloudinary.com/demo/image/upload/v1312461204/sample.jpg";
            }
        }
    }
}
