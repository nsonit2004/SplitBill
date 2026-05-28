using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SB_BusinessObjects.Entities;
using SB_Repositories.Interfaces;
using SB_Services.DTOs;
using SB_Services.Interfaces;

namespace SB_Services.Implementations
{
    public class AuthService : IAuthService
    {
        private static readonly HashSet<string> SupportedBankCodes = new(StringComparer.OrdinalIgnoreCase)
        {
            "VCB", "TCB", "MB", "BIDV", "CTG", "ACB", "VPB", "TPB", "STB", "VIB"
        };

        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;
        private readonly IBankAccountVerificationService _bankAccountVerificationService;

        public AuthService(
            IUserRepository userRepository,
            IConfiguration configuration,
            IBankAccountVerificationService bankAccountVerificationService)
        {
            _userRepository = userRepository;
            _configuration = configuration;
            _bankAccountVerificationService = bankAccountVerificationService;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
        {
            var normalizedEmail = (request.Email ?? string.Empty).Trim().ToLower();
            if (string.IsNullOrWhiteSpace(normalizedEmail))
            {
                throw new ArgumentException("Email không hợp lệ.");
            }

            request.Email = normalizedEmail;
            request.DisplayName = (request.DisplayName ?? string.Empty).Trim();
            request.BankCode = NormalizeNullable(request.BankCode)?.ToUpper();
            request.BankAccountNo = NormalizeNullable(request.BankAccountNo);
            request.BankAccountName = NormalizeNullable(request.BankAccountName);

            ValidateBankInfo(request);

            BankAccountVerificationResultDto? verificationResult = null;
            if (!string.IsNullOrWhiteSpace(request.BankCode))
            {
                verificationResult = await _bankAccountVerificationService.VerifyAsync(
                    request.BankCode!,
                    request.BankAccountNo!,
                    request.BankAccountName!);

                if (!verificationResult.IsVerified)
                {
                    throw new ArgumentException(verificationResult.Message ?? "Không xác thực được chủ tài khoản ngân hàng.");
                }
            }

            // Kiểm tra email trùng lặp
            if (await _userRepository.ExistsByEmailAsync(request.Email))
            {
                throw new ArgumentException("Email này đã được sử dụng bởi tài khoản khác.");
            }

            // Mã hóa mật khẩu
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var newUser = new User
            {
                Email = request.Email,
                PasswordHash = passwordHash,
                DisplayName = request.DisplayName,
                BankCode = request.BankCode,
                BankAccountNo = request.BankAccountNo,
                BankAccountName = request.BankAccountName,
                BankAccountVerified = verificationResult?.IsVerified ?? false,
                BankAccountVerifiedAt = verificationResult?.IsVerified == true ? DateTime.UtcNow : null,
                BankVerificationProvider = verificationResult?.Provider
            };

            await _userRepository.AddAsync(newUser);

            // Tạo token JWT
            string token = GenerateJwtToken(newUser);

            return new AuthResponseDto
            {
                Token = token,
                UserId = newUser.Id,
                Email = newUser.Email,
                DisplayName = newUser.DisplayName,
                AvatarUrl = newUser.AvatarUrl,
                BankCode = newUser.BankCode,
                BankAccountNo = newUser.BankAccountNo,
                BankAccountName = newUser.BankAccountName,
                BankAccountVerified = newUser.BankAccountVerified
            };
        }

        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
        {
            var normalizedEmail = (request.Email ?? string.Empty).Trim().ToLower();
            var user = await _userRepository.GetByEmailAsync(normalizedEmail);
            if (user == null || string.IsNullOrEmpty(user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Tài khoản hoặc mật khẩu không chính xác.");
            }

            // Kiểm tra mật khẩu
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            if (!isPasswordValid)
            {
                throw new UnauthorizedAccessException("Tài khoản hoặc mật khẩu không chính xác.");
            }

            // Tạo token JWT
            string token = GenerateJwtToken(user);

            return new AuthResponseDto
            {
                Token = token,
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                DisplayName = user.DisplayName,
                AvatarUrl = user.AvatarUrl,
                BankCode = user.BankCode,
                BankAccountNo = user.BankAccountNo,
                BankAccountName = user.BankAccountName,
                BankAccountVerified = user.BankAccountVerified
            };
        }

        public async Task<User?> GetCurrentUserAsync(string userId)
        {
            return await _userRepository.GetByIdAsync(userId);
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtKey = _configuration["Jwt:Key"] ?? "VietQRSplitBillProSuperSecuritySecretKey2026";
            var key = Encoding.ASCII.GetBytes(jwtKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                    new Claim(ClaimTypes.Name, user.DisplayName)
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                Issuer = _configuration["Jwt:Issuer"] ?? "VietQRSplitBillPro",
                Audience = _configuration["Jwt:Audience"] ?? "VietQRSplitBillProUsers",
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private static void ValidateBankInfo(RegisterRequestDto request)
        {
            var hasBankCode = !string.IsNullOrWhiteSpace(request.BankCode);
            var hasBankAccountNo = !string.IsNullOrWhiteSpace(request.BankAccountNo);
            var hasBankAccountName = !string.IsNullOrWhiteSpace(request.BankAccountName);

            if (!hasBankCode && !hasBankAccountNo && !hasBankAccountName)
            {
                return;
            }

            if (!(hasBankCode && hasBankAccountNo && hasBankAccountName))
            {
                throw new ArgumentException("Nếu khai báo thông tin ngân hàng, vui lòng nhập đầy đủ mã ngân hàng, số tài khoản và tên chủ tài khoản.");
            }

            if (!SupportedBankCodes.Contains(request.BankCode!))
            {
                throw new ArgumentException($"Mã ngân hàng '{request.BankCode}' chưa được hỗ trợ.");
            }

            if (!Regex.IsMatch(request.BankAccountNo!, @"^\d{6,20}$"))
            {
                throw new ArgumentException("Số tài khoản phải gồm 6 đến 20 chữ số.");
            }

            if (request.BankAccountName!.Length < 2)
            {
                throw new ArgumentException("Tên chủ tài khoản không hợp lệ.");
            }
        }

        private static string? NormalizeNullable(string? value)
        {
            var normalized = (value ?? string.Empty).Trim();
            return string.IsNullOrEmpty(normalized) ? null : normalized;
        }
    }
}
