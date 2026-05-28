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
    public class GroupsController : ControllerBase
    {
        private readonly IGroupService _groupService;

        public GroupsController(IGroupService groupService)
        {
            _groupService = groupService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequestDto request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var result = await _groupService.CreateGroupAsync(request, userId);
                return CreatedAtAction(nameof(GetGroupById), new { groupId = result.Id }, result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUserGroups()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var result = await _groupService.GetUserGroupsAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("{groupId}")]
        public async Task<IActionResult> GetGroupById(string groupId)
        {
            try
            {
                var result = await _groupService.GetGroupDetailAsync(groupId);
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

        [HttpPost("{groupId}/members")]
        public async Task<IActionResult> AddVirtualMember(string groupId, [FromBody] string nickname)
        {
            try
            {
                var result = await _groupService.AddVirtualMemberAsync(groupId, nickname);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{groupId}/members/{memberId}/link")]
        public async Task<IActionResult> LinkMemberAccount(string groupId, string memberId, [FromBody] string userId)
        {
            try
            {
                var result = await _groupService.LinkMemberAccountAsync(groupId, memberId, userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{groupId}/members/{memberId}")]
        public async Task<IActionResult> RemoveMember(string groupId, string memberId)
        {
            try
            {
                await _groupService.RemoveMemberAsync(groupId, memberId);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
