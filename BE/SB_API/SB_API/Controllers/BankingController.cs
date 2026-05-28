using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SB_Services.DTOs;
using SB_Services.Interfaces;
using SB_Repositories.Interfaces;

namespace SB_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BankingController : ControllerBase
    {
        private readonly IBankAccountVerificationService _bankAccountVerificationService;
        private readonly ISettlementService _settlementService;
        private readonly IConfiguration _configuration;
        private readonly IUserRepository _userRepository;

        public BankingController(
            IBankAccountVerificationService bankAccountVerificationService,
            ISettlementService settlementService,
            IConfiguration configuration,
            IUserRepository userRepository)
        {
            _bankAccountVerificationService = bankAccountVerificationService;
            _settlementService = settlementService;
            _configuration = configuration;
            _userRepository = userRepository;
        }

        [Authorize]
        [HttpPut("me/bank-info")]
        public async Task<IActionResult> UpdateMyBankInfo([FromBody] RegisterRequestDto request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null) return NotFound(new { message = "Người dùng không tồn tại." });

                var bankCode = (request.BankCode ?? string.Empty).Trim().ToUpper();
                var bankAccountNo = (request.BankAccountNo ?? string.Empty).Trim();
                var bankAccountName = (request.BankAccountName ?? string.Empty).Trim();

                var result = await _bankAccountVerificationService.VerifyAsync(
                    bankCode,
                    bankAccountNo,
                    bankAccountName
                );

                if (!result.IsVerified)
                {
                    return BadRequest(new { message = result.Message ?? "Không xác thực được chủ tài khoản ngân hàng." });
                }

                user.BankCode = bankCode;
                user.BankAccountNo = bankAccountNo;
                user.BankAccountName = result.ResolvedAccountName ?? bankAccountName;
                user.BankAccountVerified = true;
                user.BankAccountVerifiedAt = DateTime.UtcNow;
                user.BankVerificationProvider = result.Provider;

                await _userRepository.UpdateAsync(user);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPost("webhooks/transfer")]
        public async Task<IActionResult> HandleTransferWebhook(
            [FromBody] BankTransferWebhookDto payload,
            [FromHeader(Name = "X-Webhook-Secret")] string? webhookSecret)
        {
            try
            {
                var expectedSecret = _configuration["Banking:WebhookSecret"];
                if (string.IsNullOrWhiteSpace(expectedSecret) || expectedSecret != webhookSecret)
                {
                    return Unauthorized(new { message = "Webhook secret không hợp lệ." });
                }

                var result = await _settlementService.CompleteByWebhookAsync(payload);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
