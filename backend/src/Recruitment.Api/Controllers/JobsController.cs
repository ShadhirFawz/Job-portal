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

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetJob(Guid id, [FromQuery] bool publicOnly = false)
    {
        var job = await _jobService.GetJobByIdAsync(id, publicOnly);
        if (job == null)
        {
            return NotFound(new { message = "Job not found." });
        }
        return Ok(job);
    }

    [HttpGet("organization/{organizationId:guid}")]
    public async Task<IActionResult> GetOrganizationJobs(Guid organizationId)
    {
        var list = await _jobService.GetOrganizationJobsAsync(organizationId);
        return Ok(list);
    }
}

