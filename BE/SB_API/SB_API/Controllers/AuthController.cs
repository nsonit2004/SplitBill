using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SB_Services.DTOs;
using SB_Services.Interfaces;

namespace SB_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            try
            {
                var result = await _authService.RegisterAsync(request);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi hệ thống khi đăng ký." });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            try
            {
                var result = await _authService.LoginAsync(request);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi hệ thống khi đăng nhập." });
            }
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Không tìm thấy thông tin đăng nhập." });
                }

                var user = await _authService.GetCurrentUserAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "Người dùng không tồn tại." });
                }

                return Ok(new
                {
                    userId = user.Id,
                    email = user.Email,
                    displayName = user.DisplayName,
                    avatarUrl = user.AvatarUrl,
                    bankCode = user.BankCode,
                    bankAccountNo = user.BankAccountNo,
                    bankAccountName = user.BankAccountName
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi hệ thống." });
            }
        }
    }
}
