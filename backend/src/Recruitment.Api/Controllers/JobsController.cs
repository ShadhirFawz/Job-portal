using Microsoft.AspNetCore.Mvc;
using Recruitment.Application.DTOs;
using Recruitment.Application.Interfaces;

namespace Recruitment.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly IJobService _jobService;

    public JobsController(IJobService jobService)
    {
        _jobService = jobService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPublishedJobs([FromQuery] JobFilterRequest request)
    {
        var result = await _jobService.GetPublishedJobsAsync(request);
        return Ok(result);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetJob(string slug, [FromQuery] bool publicOnly = false)
    {
        var job = await _jobService.GetJobBySlugAsync(slug, publicOnly);
        if (job == null) return NotFound(new { message = "Job not found." });
        return Ok(job);
    }

    [HttpGet("organization/{slug}")]
    public async Task<IActionResult> GetOrganizationJobs(string slug)
    {
        var list = await _jobService.GetOrganizationJobsBySlugAsync(slug);
        return Ok(list);
    }

    [HttpPut("{slug}")]
    public async Task<IActionResult> UpdateJob(string slug, [FromQuery] Guid userId, [FromBody] UpdateJobRequest request)
    {
        try
        {
            var jobId = await _jobService.GetJobIdBySlugAsync(slug);
            if (jobId == Guid.Empty) return NotFound(new { message = "Job not found." });
            var updated = await _jobService.UpdateJobAsync(jobId, userId, request);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("{slug}/publish")]
    public async Task<IActionResult> PublishJob(string slug, [FromQuery] Guid userId)
    {
        try
        {
            var jobId = await _jobService.GetJobIdBySlugAsync(slug);
            if (jobId == Guid.Empty) return NotFound(new { message = "Job not found." });
            var published = await _jobService.PublishJobAsync(jobId, userId);
            return Ok(published);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }
}

