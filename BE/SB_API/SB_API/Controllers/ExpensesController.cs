using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
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
                var result = await _expenseService.GetGroupExpensesAsync(groupId);
                return Ok(result);
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

        [HttpGet("{expenseId}")]
        public async Task<IActionResult> GetExpenseById(string expenseId)
        {
            try
            {
                var result = await _expenseService.GetExpenseDetailAsync(expenseId);
                return Ok(result);
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

        [HttpDelete("{expenseId}")]
        public async Task<IActionResult> DeleteExpense(string expenseId)
        {
            try
            {
                await _expenseService.DeleteExpenseAsync(expenseId);
                return NoContent();
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
