using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using SB_Services.DTOs;
using SB_Services.Interfaces;

namespace SB_Services.Implementations
{
    public class BankAccountVerificationService : IBankAccountVerificationService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public BankAccountVerificationService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<BankAccountVerificationResultDto> VerifyAsync(string bankCode, string bankAccountNo, string bankAccountName)
        {
            var mode = (_configuration["Banking:ProviderMode"] ?? "Mock").Trim();
            if (string.Equals(mode, "External", StringComparison.OrdinalIgnoreCase))
            {
                return await VerifyViaExternalProviderAsync(bankCode, bankAccountNo, bankAccountName);
            }

            return VerifyViaMockProvider(bankCode, bankAccountNo, bankAccountName);
        }

        private async Task<BankAccountVerificationResultDto> VerifyViaExternalProviderAsync(string bankCode, string bankAccountNo, string bankAccountName)
        {
            var verifyApiUrl = _configuration["Banking:VerifyApiUrl"];
            if (string.IsNullOrWhiteSpace(verifyApiUrl))
            {
                return new BankAccountVerificationResultDto
                {
                    IsVerified = false,
                    Message = "Chưa cấu hình Banking:VerifyApiUrl cho chế độ External.",
                    Provider = "External"
                };
            }

            var requestPayload = JsonSerializer.Serialize(new
            {
                bankCode,
                accountNo = bankAccountNo,
                accountName = bankAccountName
            });

            var request = new HttpRequestMessage(HttpMethod.Post, verifyApiUrl)
            {
                Content = new StringContent(requestPayload, Encoding.UTF8, "application/json")
            };

            var apiKey = _configuration["Banking:ApiKey"];
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                request.Headers.Add("X-API-Key", apiKey);
            }

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return new BankAccountVerificationResultDto
                {
                    IsVerified = false,
                    Message = $"Nhà cung cấp xác thực trả về lỗi HTTP {(int)response.StatusCode}.",
                    Provider = "External"
                };
            }

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            var isVerified = TryReadBool(root, "isVerified") || TryReadBool(root, "verified") || TryReadBool(root, "success");
            var resolvedName = TryReadString(root, "resolvedAccountName") ?? TryReadString(root, "accountName");
            var message = TryReadString(root, "message");

            return new BankAccountVerificationResultDto
            {
                IsVerified = isVerified,
                ResolvedAccountName = resolvedName,
                Message = message ?? (isVerified ? "Xác thực ngân hàng thành công." : "Không xác thực được chủ tài khoản."),
                Provider = "External"
            };
        }

        private static BankAccountVerificationResultDto VerifyViaMockProvider(string bankCode, string bankAccountNo, string bankAccountName)
        {
            var hasValidShape = !string.IsNullOrWhiteSpace(bankCode)
                                && !string.IsNullOrWhiteSpace(bankAccountNo)
                                && !string.IsNullOrWhiteSpace(bankAccountName);

            return new BankAccountVerificationResultDto
            {
                IsVerified = hasValidShape,
                ResolvedAccountName = hasValidShape ? bankAccountName : null,
                Message = hasValidShape
                    ? "Mock verify: đã xác thực hình thức tài khoản."
                    : "Mock verify: thiếu dữ liệu để xác thực.",
                Provider = "Mock"
            };
        }

        private static bool TryReadBool(JsonElement root, string propName)
        {
            if (!root.TryGetProperty(propName, out var value))
            {
                return false;
            }

            return value.ValueKind == JsonValueKind.True ||
                   (value.ValueKind == JsonValueKind.String && bool.TryParse(value.GetString(), out var parsed) && parsed);
        }

        private static string? TryReadString(JsonElement root, string propName)
        {
            return root.TryGetProperty(propName, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
        }
    }
}
