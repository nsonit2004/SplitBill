using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SB_Services.DTOs;
using SB_Services.Interfaces;

namespace SB_API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ExpensesController : ControllerBase
    {
        private readonly IExpenseService _expenseService;

        public ExpensesController(IExpenseService expenseService)
        {
            _expenseService = expenseService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadImage(IFormFile file, [FromServices] ICloudinaryService cloudinaryService)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "Không tìm thấy file để upload." });
                }

                using var stream = file.OpenReadStream();
                var (imageUrl, uploadError) = await cloudinaryService.UploadImageStreamAsync(stream, file.FileName, "expenses");
                if (!string.IsNullOrEmpty(uploadError))
                {
                    return BadRequest(new { message = uploadError });
                }

                return Ok(new ImageUploadResponseDto
                {
                    ImageUrl = imageUrl,
                    Provider = "Cloudinary"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("scan-receipt")]
        public async Task<IActionResult> ScanReceipt(IFormFile file, [FromServices] IOcrService ocrService)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "Không tìm thấy file hóa đơn để quét." });
                }

                var mimeType = file.ContentType;
                if (string.IsNullOrEmpty(mimeType))
                {
                    mimeType = "image/jpeg";
                }

                using var stream = file.OpenReadStream();
                var (result, errorMessage) = await ocrService.ScanReceiptAsync(stream, mimeType);

                if (!string.IsNullOrEmpty(errorMessage))
                {
                    return BadRequest(new { message = errorMessage });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("group/{groupId}")]
        public async Task<IActionResult> CreateExpense(string groupId, [FromBody] CreateExpenseRequestDto request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var result = await _expenseService.CreateExpenseAsync(groupId, request, userId);
                return CreatedAtAction(nameof(GetExpenseById), new { expenseId = result.Id }, result);
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

        [HttpGet("group/{groupId}")]
        public async Task<IActionResult> GetGroupExpenses(string groupId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var result = await _expenseService.GetGroupExpensesAsync(groupId, userId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("{expenseId}")]
        public async Task<IActionResult> GetExpenseById(string expenseId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var result = await _expenseService.GetExpenseDetailAsync(expenseId, userId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpDelete("{expenseId}")]
        public async Task<IActionResult> DeleteExpense(string expenseId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                await _expenseService.DeleteExpenseAsync(expenseId, userId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
