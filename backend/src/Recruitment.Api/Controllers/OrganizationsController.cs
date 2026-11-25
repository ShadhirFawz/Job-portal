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

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetOrganization(string slug)
    {
        var org = await _organizationService.GetOrganizationAsync(slug);
        if (org == null) return NotFound(new { message = "Organization not found." });
        return Ok(org);
    }

    [HttpGet("user/{userId:guid}")]
    public async Task<IActionResult> GetMyOrganizations(Guid userId)
    {
        var orgs = await _organizationService.GetMyOrganizationsAsync(userId);
        return Ok(orgs);
    }

    [HttpGet("{slug}/is-admin")]
    public async Task<IActionResult> CheckIsOrgAdmin(
        string slug,
        [FromQuery] Guid userId,
        [FromQuery] string? userRole = null,
        [FromQuery] Guid? orgCreatedBy = null)
    {
        var org = await _organizationService.GetOrganizationAsync(slug);
        if (org == null) return NotFound(new { message = "Organization not found." });
        var isAdmin = await _organizationService.CheckIsOrgAdminAsync(org.Id, userId, userRole, orgCreatedBy);
        return Ok(new { isAdmin });
    }

    [HttpGet("{slug}/is-staff")]
    public async Task<IActionResult> CheckIsOrgAdminOrStaff(
        string slug,
        [FromQuery] Guid userId,
        [FromQuery] string? userRole = null,
        [FromQuery] Guid? orgCreatedBy = null)
    {
        var org = await _organizationService.GetOrganizationAsync(slug);
        if (org == null) return NotFound(new { message = "Organization not found." });
        var isStaff = await _organizationService.CheckIsOrgAdminOrStaffAsync(org.Id, userId, userRole, orgCreatedBy);
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
            return CreatedAtAction(nameof(GetOrganization), new { slug = org.Slug ?? org.Id.ToString() }, org);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{slug}")]
    public async Task<IActionResult> UpdateOrganization(
        string slug,
        [FromBody] UpdateOrganizationRequest request)
    {
        try
        {
            var org = await _organizationService.GetOrganizationAsync(slug);
            if (org == null) return NotFound(new { message = "Organization not found." });
            var updated = await _organizationService.UpdateOrganizationAsync(org.Id, request);
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

    [HttpPost("{slug}/email-verification")]
    public async Task<IActionResult> CreateEmailVerification(
        string slug,
        [FromBody] CreateEmailVerificationRequest request,
        [FromQuery] Guid userId)
    {
        try
        {
            var org = await _organizationService.GetOrganizationAsync(slug);
            if (org == null) return NotFound(new { message = "Organization not found." });
            var result = await _organizationService.CreateEmailVerificationTokenAsync(
                org.Id, request.Email, userId, request.Origin);
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

