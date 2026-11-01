using Microsoft.AspNetCore.Mvc;
using Recruitment.Application.DTOs;
using Recruitment.Application.Interfaces;

namespace Recruitment.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ResumesController : ControllerBase
{
    private readonly IResumeService _resumeService;

    public ResumesController(IResumeService resumeService)
    {
        _resumeService = resumeService;
    }

    [HttpGet("user/{userId:guid}")]
    public async Task<IActionResult> FetchUserResumes(Guid userId)
    {
        var list = await _resumeService.FetchUserResumesAsync(userId);
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> FetchResumeById(Guid id)
    {
        var resume = await _resumeService.FetchResumeByIdAsync(id);
        if (resume == null) return NotFound(new { message = "Resume not found." });
        return Ok(resume);
    }

    [HttpPost]
    public async Task<IActionResult> CreateResume([FromBody] CreateResumeRequest request)
    {
        try
        {
            var created = await _resumeService.CreateResumeAsync(request);
            return CreatedAtAction(nameof(FetchResumeById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateResume(Guid id, [FromBody] UpdateResumeRequest request)
    {
        try
        {
            var updated = await _resumeService.UpdateResumeAsync(id, request);
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

    [HttpPut("{id:guid}/primary")]
    public async Task<IActionResult> SetPrimaryResume(Guid id)
    {
        try
        {
            await _resumeService.SetPrimaryResumeAsync(id);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteResume(Guid id)
    {
        try
        {
            await _resumeService.DeleteResumeAsync(id);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("download-url")]
    public async Task<IActionResult> GetDownloadUrl([FromQuery] string filePath, [FromQuery] int expiresIn = 3600)
    {
        var url = await _resumeService.GetDownloadUrlAsync(filePath, expiresIn);
        if (string.IsNullOrEmpty(url)) return BadRequest(new { message = "Could not generate download URL." });
        return Ok(new SignedUrlResponse { SignedUrl = url });
    }
}

