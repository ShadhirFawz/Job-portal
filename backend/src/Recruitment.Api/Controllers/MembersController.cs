using Microsoft.AspNetCore.Mvc;
using Recruitment.Application.DTOs;
using Recruitment.Application.Interfaces;

namespace Recruitment.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MembersController : ControllerBase
{
    private readonly IMemberService _memberService;

    public MembersController(IMemberService memberService)
    {
        _memberService = memberService;
    }

    [HttpGet("organization/{organizationId:guid}")]
    public async Task<IActionResult> FetchMembers(Guid organizationId)
    {
        var members = await _memberService.FetchMembersAsync(organizationId);
        return Ok(members);
    }

    [HttpPost("invite")]
    public async Task<IActionResult> InviteMember(
        [FromBody] InviteMemberRequest request,
        [FromQuery] Guid inviterId)
    {
        try
        {
            await _memberService.InviteMemberAsync(request, inviterId);
            return Ok(new { success = true, message = "Invitation sent successfully." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/accept")]
    public async Task<IActionResult> AcceptMember(Guid id)
    {
        try
        {
            await _memberService.AcceptMemberAsync(id);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> RejectMember(Guid id, [FromBody] RejectMemberRequest? request)
    {
        try
        {
            await _memberService.RejectMemberAsync(id, request?.Reason);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/revoke")]
    public async Task<IActionResult> RevokeMember(Guid id, [FromQuery] Guid revokedByUserId)
    {
        try
        {
            await _memberService.RevokeMemberAsync(id, revokedByUserId);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/reinvite")]
    public async Task<IActionResult> ReinviteMember(Guid id)
    {
        try
        {
            await _memberService.ReinviteMemberAsync(id);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteMemberRecord(Guid id)
    {
        try
        {
            await _memberService.DeleteMemberRecordAsync(id);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}/role")]
    public async Task<IActionResult> UpdateMemberRole(Guid id, [FromBody] UpdateMemberRoleRequest request)
    {
        try
        {
            await _memberService.UpdateMemberRoleAsync(id, request.Role);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}/details")]
    public async Task<IActionResult> UpdateMemberDetails(Guid id, [FromBody] UpdateMemberDetailsRequest request)
    {
        try
        {
            await _memberService.UpdateMemberDetailsAsync(id, request.Title, request.Department);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("invitations/user/{userId:guid}")]
    public async Task<IActionResult> FetchMyInvitations(Guid userId)
    {
        var invitations = await _memberService.FetchMyInvitationsAsync(userId);
        return Ok(invitations);
    }

    [HttpPost("invitations/{id:guid}/accept")]
    public async Task<IActionResult> AcceptInvitation(Guid id)
    {
        try
        {
            await _memberService.AcceptInvitationAsync(id);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("invitations/{id:guid}/decline")]
    public async Task<IActionResult> DeclineInvitation(Guid id)
    {
        try
        {
            await _memberService.DeclineInvitationAsync(id);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

