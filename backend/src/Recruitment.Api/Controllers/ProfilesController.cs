using Microsoft.AspNetCore.Mvc;
using Recruitment.Application.DTOs;
using Recruitment.Application.Interfaces;

namespace Recruitment.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProfilesController : ControllerBase
{
    private readonly IProfileService _profileService;

    public ProfilesController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetUserProfile(Guid userId)
    {
        var profile = await _profileService.GetUserProfileAsync(userId);
        if (profile == null) return NotFound(new { message = "User profile not found." });
        return Ok(profile);
    }

    [HttpGet("username/{username}")]
    public async Task<IActionResult> GetUserProfileByUsername(string username)
    {
        var profile = await _profileService.GetUserProfileByUsernameAsync(username);
        if (profile == null) return NotFound(new { message = $"User @{username} not found." });
        return Ok(profile);
    }

    [HttpGet("check-username")]
    public async Task<IActionResult> CheckUsername([FromQuery] string username, [FromQuery] Guid? currentUserId)
    {
        var result = await _profileService.CheckUsernameAvailabilityAsync(username, currentUserId);
        return Ok(result);
    }

    [HttpPut("{userId:guid}")]
    public async Task<IActionResult> UpdateUserProfile(Guid userId, [FromBody] UpdateProfileRequest request)
    {
        try
        {
            var updated = await _profileService.UpdateUserProfileAsync(userId, request);
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
}

