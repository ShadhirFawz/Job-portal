using Microsoft.AspNetCore.Mvc;
using Recruitment.Application.DTOs;
using Recruitment.Application.Interfaces;

namespace Recruitment.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApplicationsController : ControllerBase
{
    private readonly IApplicationService _applicationService;

    public ApplicationsController(IApplicationService applicationService)
    {
        _applicationService = applicationService;
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

    [HttpGet("job/{jobId:guid}")]
    public async Task<IActionResult> FetchApplicationsForJob(Guid jobId)
    {
        var list = await _applicationService.FetchApplicationsForJobAsync(jobId);
        return Ok(list);
    }

    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateApplicationStatus(Guid id, [FromBody] UpdateApplicationStatusRequest request)
    {
        try
        {
            await _applicationService.UpdateApplicationStatusAsync(id, request.Status);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

