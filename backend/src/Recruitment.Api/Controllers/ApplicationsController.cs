using Microsoft.AspNetCore.Mvc;
using Recruitment.Application.DTOs;
using Recruitment.Application.Interfaces;

namespace Recruitment.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApplicationsController : ControllerBase
{
    private readonly IApplicationService _applicationService;
    private readonly IJobService _jobService;

    public ApplicationsController(IApplicationService applicationService, IJobService jobService)
    {
        _applicationService = applicationService;
        _jobService = jobService;
    }

    [HttpGet("check")]
    public async Task<IActionResult> CheckUserApplied([FromQuery] Guid jobId, [FromQuery] Guid userId)
    {
        var applied = await _applicationService.CheckUserAppliedAsync(jobId, userId);
        return Ok(new { applied });
    }

    [HttpGet("user")]
    public async Task<IActionResult> FetchUserApplication([FromQuery] Guid jobId, [FromQuery] Guid userId)
    {
        var app = await _applicationService.FetchUserApplicationAsync(jobId, userId);
        return Ok(app);
    }

    [HttpPost]
    public async Task<IActionResult> ApplyForJob([FromBody] CreateApplicationRequest request)
    {
        try
        {
            var created = await _applicationService.ApplyForJobAsync(request);
            return Ok(created);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("my")]
    public async Task<IActionResult> FetchMyApplications([FromQuery] Guid userId)
    {
        if (userId == Guid.Empty) return BadRequest(new { message = "userId is required." });
        var list = await _applicationService.FetchMyApplicationsAsync(userId);
        return Ok(list);
    }

    [HttpPost("{slug}/withdraw")]
    public async Task<IActionResult> WithdrawApplication(string slug, [FromQuery] Guid userId)
    {
        if (userId == Guid.Empty) return BadRequest(new { message = "userId is required." });
        var applicationId = await _applicationService.GetApplicationIdBySlugAsync(slug);
        if (applicationId == Guid.Empty) return NotFound(new { message = "Application not found." });

        try
        {
            await _applicationService.WithdrawApplicationAsync(applicationId, userId);
            return Ok(new { success = true });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("job/{slug}")]
    public async Task<IActionResult> FetchApplicationsForJob(string slug)
    {
        var jobId = await _jobService.GetJobIdBySlugAsync(slug);
        var list = await _applicationService.FetchApplicationsForJobAsync(jobId);
        return Ok(list);
    }

    [HttpPut("{slug}/status")]
    public async Task<IActionResult> UpdateApplicationStatus(string slug, [FromBody] UpdateApplicationStatusRequest request)
    {
        var applicationId = await _applicationService.GetApplicationIdBySlugAsync(slug);
        if (applicationId == Guid.Empty) return NotFound(new { message = "Application not found." });
        await _applicationService.UpdateApplicationStatusAsync(applicationId, request.Status);
        return Ok(new { success = true });
    }
}

