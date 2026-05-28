using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using SB_Services.DTOs;
using SB_Services.Interfaces;

namespace SB_Services.Implementations
{
    public class GeminiOcrService : IOcrService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public GeminiOcrService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<(OcrScanResultDto? Result, string? ErrorMessage)> ScanReceiptAsync(Stream imageStream, string mimeType)
        {
            var apiKey = _configuration["Gemini:ApiKey"];

            // 1. Chế độ giả lập (Mock Mode) nếu chưa cấu hình API Key thực tế
            if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "YOUR_GEMINI_API_KEY")
            {
                await Task.Delay(1500); // Giả lập độ trễ mạng
                return (new OcrScanResultDto
                {
                    MerchantName = "Lẩu Băng Chuyền Kichi Kichi",
                    Date = DateTime.Now.ToString("yyyy-MM-dd"),
                    Tax = 66600m, // 10% VAT
                    ServiceCharge = 20000m,
                    TotalAmount = 752600m,
                    Items = new List<OcrLineItemDto>
                    {
                        new OcrLineItemDto { Name = "Buffet Lẩu Băng Chuyền", Quantity = 2, UnitPrice = 299000, TotalPrice = 598000 },
                        new OcrLineItemDto { Name = "Nước Ngọt Coca Cola", Quantity = 2, UnitPrice = 29000, TotalPrice = 58000 },
                        new OcrLineItemDto { Name = "Khăn lạnh", Quantity = 2, UnitPrice = 5000, TotalPrice = 10000 }
                    }
                }, null);
            }

            try
            {
                // 2. Chuyển stream ảnh thành base64
                using var memoryStream = new MemoryStream();
                await imageStream.CopyToAsync(memoryStream);
                var base64Image = Convert.ToBase64String(memoryStream.ToArray());

                // 3. Chuẩn bị schema cho đầu ra JSON
                var responseSchema = new
                {
                    type = "OBJECT",
                    properties = new
                    {
                        merchantName = new { type = "STRING", description = "Tên nhà hàng hoặc cửa hàng trên hóa đơn" },
                        date = new { type = "STRING", description = "Ngày hóa đơn định dạng YYYY-MM-DD" },
                        tax = new { type = "NUMBER", description = "Tiền thuế VAT" },
                        serviceCharge = new { type = "NUMBER", description = "Phí dịch vụ nếu có" },
                        totalAmount = new { type = "NUMBER", description = "Tổng tiền cần thanh toán của hóa đơn" },
                        items = new
                        {
                            type = "ARRAY",
                            items = new
                            {
                                type = "OBJECT",
                                properties = new
                                {
                                    name = new { type = "STRING", description = "Tên món ăn hoặc sản phẩm" },
                                    quantity = new { type = "NUMBER", description = "Số lượng" },
                                    unitPrice = new { type = "NUMBER", description = "Đơn giá" },
                                    totalPrice = new { type = "NUMBER", description = "Thành tiền" }
                                },
                                required = new[] { "name", "quantity", "unitPrice", "totalPrice" }
                            }
                        }
                    },
                    required = new[] { "merchantName", "totalAmount", "items" }
                };

                // 4. Chuẩn bị request payload cho Gemini API
                var requestPayload = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new object[]
                            {
                                new { text = "Hãy trích xuất thông tin hóa đơn này. Đọc đúng tên món ăn tiếng Việt, số lượng, đơn giá và tổng tiền. Trả về đúng định dạng JSON được định nghĩa trong schema." },
                                new
                                {
                                    inlineData = new
                                    {
                                        mimeType = mimeType,
                                        data = base64Image
                                    }
                                }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        responseMimeType = "application/json",
                        responseSchema = responseSchema
                    }
                };

                // 5. Gửi request đến Gemini API
                var model = _configuration["Gemini:Model"] ?? "gemini-2.5-flash";
                var apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
                var response = await _httpClient.PostAsJsonAsync(apiUrl, requestPayload);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return (null, $"Lỗi từ Gemini API: {response.StatusCode} - {errorContent}");
                }

                // 6. Parse kết quả trả về
                var geminiResponse = await response.Content.ReadFromJsonAsync<GeminiResponseDto>();
                var rawJsonText = geminiResponse?.Candidates?[0]?.Content?.Parts?[0]?.Text;

                if (string.IsNullOrWhiteSpace(rawJsonText))
                {
                    return (null, "Không thể đọc văn bản JSON từ phản hồi của Gemini.");
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<OcrScanResultDto>(rawJsonText, options);

                if (result == null)
                {
                    return (null, "Không thể giải tuần tự hóa kết quả quét hóa đơn.");
                }

                return (result, null);
            }
            catch (Exception ex)
            {
                return (null, $"Lỗi xử lý quét hóa đơn AI: {ex.Message}");
            }
        }
    }

    #region Gemini API DTOs
    public class GeminiResponseDto
    {
        public List<GeminiCandidateDto>? Candidates { get; set; }
    }

    public class GeminiCandidateDto
    {
        public GeminiContentDto? Content { get; set; }
    }

    public class GeminiContentDto
    {
        public List<GeminiPartDto>? Parts { get; set; }
    }

    public class GeminiPartDto
    {
        public string? Text { get; set; }
    }
    #endregion
}
