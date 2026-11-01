using Microsoft.AspNetCore.Mvc;
using Recruitment.Application.DTOs;
using Recruitment.Application.Interfaces;

namespace Recruitment.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrganizationsController : ControllerBase
{
    private readonly IOrganizationService _organizationService;

    public OrganizationsController(IOrganizationService organizationService)
    {
        _organizationService = organizationService;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOrganization(Guid id)
    {
        var org = await _organizationService.GetOrganizationAsync(id);
        if (org == null) return NotFound(new { message = "Organization not found." });
        return Ok(org);
    }

    [HttpGet("user/{userId:guid}")]
    public async Task<IActionResult> GetMyOrganizations(Guid userId)
    {
        var orgs = await _organizationService.GetMyOrganizationsAsync(userId);
        return Ok(orgs);
    }

    [HttpGet("{id:guid}/is-admin")]
    public async Task<IActionResult> CheckIsOrgAdmin(
        Guid id,
        [FromQuery] Guid userId,
        [FromQuery] string? userRole = null,
        [FromQuery] Guid? orgCreatedBy = null)
    {
        var isAdmin = await _organizationService.CheckIsOrgAdminAsync(id, userId, userRole, orgCreatedBy);
        return Ok(new { isAdmin });
    }

    [HttpGet("{id:guid}/is-staff")]
    public async Task<IActionResult> CheckIsOrgAdminOrStaff(
        Guid id,
        [FromQuery] Guid userId,
        [FromQuery] string? userRole = null,
        [FromQuery] Guid? orgCreatedBy = null)
    {
        var isStaff = await _organizationService.CheckIsOrgAdminOrStaffAsync(id, userId, userRole, orgCreatedBy);
        return Ok(new { isStaff });
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrganization(
        [FromBody] CreateOrganizationRequest request,
        [FromQuery] Guid userId)
    {
        try
        {
            var org = await _organizationService.CreateOrganizationAsync(request, userId);
            return CreatedAtAction(nameof(GetOrganization), new { id = org.Id }, org);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateOrganization(
        Guid id,
        [FromBody] UpdateOrganizationRequest request)
    {
        try
        {
            var updated = await _organizationService.UpdateOrganizationAsync(id, request);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/email-verification")]
    public async Task<IActionResult> CreateEmailVerification(
        Guid id,
        [FromBody] CreateEmailVerificationRequest request,
        [FromQuery] Guid userId)
    {
        try
        {
            var result = await _organizationService.CreateEmailVerificationTokenAsync(
                id, request.Email, userId, request.Origin);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        try
        {
            var result = await _organizationService.VerifyEmailTokenAsync(request.Token);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

