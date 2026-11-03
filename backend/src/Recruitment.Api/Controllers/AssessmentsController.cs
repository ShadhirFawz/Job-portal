using Microsoft.AspNetCore.Mvc;
using Recruitment.Application.DTOs;
using Recruitment.Application.Interfaces;

namespace Recruitment.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AssessmentsController : ControllerBase
{
    private readonly IAssessmentService _assessmentService;

    public AssessmentsController(IAssessmentService assessmentService)
    {
        _assessmentService = assessmentService;
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartAssessment(
        [FromQuery] Guid userId,
        [FromBody] StartAssessmentRequest request)
    {
        try
        {
            var attempt = await _assessmentService.StartAssessmentAsync(userId, request);
            return Ok(attempt);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
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

    [HttpGet("{attemptId:guid}")]
    public async Task<IActionResult> GetAttempt(Guid attemptId, [FromQuery] Guid userId)
    {
        var attempt = await _assessmentService.GetAttemptByIdAsync(attemptId, userId);
        if (attempt == null) return NotFound(new { message = "Assessment attempt not found." });
        return Ok(attempt);
    }

    [HttpGet("stage/{selectionProcessId:guid}")]
    public async Task<IActionResult> GetStageAttempt(Guid selectionProcessId, [FromQuery] Guid userId)
    {
        var attempt = await _assessmentService.GetUserStageAttemptAsync(selectionProcessId, userId);
        return Ok(attempt);
    }

    [HttpPost("submit")]
    public async Task<IActionResult> SubmitAssessment(
        [FromQuery] Guid userId,
        [FromBody] SubmitAssessmentRequest request)
    {
        try
        {
            var result = await _assessmentService.SubmitAssessmentAsync(userId, request);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

