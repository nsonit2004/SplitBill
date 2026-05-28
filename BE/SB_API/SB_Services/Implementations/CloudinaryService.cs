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
        private readonly bool _isConfigured;

        public CloudinaryService(IConfiguration configuration)
        {
            var cloudName = configuration["Cloudinary:CloudName"];
            var apiKey = configuration["Cloudinary:ApiKey"];
            var apiSecret = configuration["Cloudinary:ApiSecret"];

            _isConfigured = !string.IsNullOrWhiteSpace(cloudName)
                && !string.IsNullOrWhiteSpace(apiKey)
                && !string.IsNullOrWhiteSpace(apiSecret);

            if (_isConfigured)
            {
                var account = new Account(cloudName, apiKey, apiSecret);
                _cloudinary = new Cloudinary(account);
            }
        }

        public async Task<(string? ImageUrl, string? ErrorMessage)> UploadImageAsync(string base64Image, string folderName)
        {
            if (!_isConfigured || _cloudinary == null)
            {
                return ("https://images.unsplash.com/photo-1554224155-8d04cb21cd6c?q=80&w=500", null);
            }

            try
            {
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(Guid.NewGuid().ToString("N"), base64Image),
                    Folder = folderName
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                if (uploadResult.Error != null)
                {
                    return (null, $"Upload Cloudinary thất bại: {uploadResult.Error.Message}");
                }

                if (uploadResult.SecureUrl == null)
                {
                    return (null, "Cloudinary không trả về URL ảnh.");
                }

                return (uploadResult.SecureUrl.ToString(), null);
            }
            catch (Exception ex)
            {
                return (null, $"Lỗi upload ảnh: {ex.Message}");
            }
        }

        public async Task<(string? ImageUrl, string? ErrorMessage)> UploadImageStreamAsync(Stream fileStream, string fileName, string folderName)
        {
            if (!_isConfigured || _cloudinary == null)
            {
                return ("https://images.unsplash.com/photo-1554224155-8d04cb21cd6c?q=80&w=500", null);
            }

            try
            {
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(fileName, fileStream),
                    Folder = folderName
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                if (uploadResult.Error != null)
                {
                    return (null, $"Upload Cloudinary thất bại: {uploadResult.Error.Message}");
                }

                if (uploadResult.SecureUrl == null)
                {
                    return (null, "Cloudinary không trả về URL ảnh.");
                }

                return (uploadResult.SecureUrl.ToString(), null);
            }
            catch (Exception ex)
            {
                return (null, $"Lỗi upload ảnh: {ex.Message}");
            }
        }
    }
}
