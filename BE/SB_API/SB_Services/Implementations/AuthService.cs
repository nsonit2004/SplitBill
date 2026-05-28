using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
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
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;

        public AuthService(IUserRepository userRepository, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _configuration = configuration;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
        {
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
                BankAccountName = request.BankAccountName
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
                AvatarUrl = newUser.AvatarUrl
            };
        }

        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
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
                AvatarUrl = user.AvatarUrl
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
    }
}
