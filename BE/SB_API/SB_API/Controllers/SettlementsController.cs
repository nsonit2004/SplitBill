using System;
using System.Collections.Generic;
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
    public class SettlementsController : ControllerBase
    {
        private readonly ISettlementService _settlementService;

        public SettlementsController(ISettlementService settlementService)
        {
            _settlementService = settlementService;
        }

        [HttpGet("group/{groupId}/balances")]
        public async Task<IActionResult> GetGroupBalances(string groupId)
        {
            try
            {
                var result = await _settlementService.GetGroupBalancesAsync(groupId);
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

        [HttpGet("group/{groupId}/simplified")]
        public async Task<IActionResult> GetGroupSimplifiedDebts(string groupId)
        {
            try
            {
                var result = await _settlementService.GetGroupSimplifiedDebtsAsync(groupId);
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

        [HttpPost("group/{groupId}/transactions")]
        public async Task<IActionResult> CreateSettleTransaction(string groupId, [FromBody] CreateSettleTransactionRequestDto request)
        {
            try
            {
                var result = await _settlementService.CreateSettleTransactionAsync(groupId, request);
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

        [HttpPost("transactions/{transactionId}/complete")]
        public async Task<IActionResult> CompleteSettleTransaction(string transactionId)
        {
            try
            {
                var result = await _settlementService.CompleteSettleTransactionAsync(transactionId);
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

        [HttpPost("transactions/{transactionId}/proof")]
        public async Task<IActionResult> UploadProofImage(string transactionId, [FromBody] string proofImageUrl)
        {
            try
            {
                var result = await _settlementService.UploadProofImageAsync(transactionId, proofImageUrl);
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

        [HttpGet("group/{groupId}/history")]
        public async Task<IActionResult> GetGroupTransactionsHistory(string groupId)
        {
            try
            {
                var result = await _settlementService.GetGroupTransactionsHistoryAsync(groupId);
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
    }
}
