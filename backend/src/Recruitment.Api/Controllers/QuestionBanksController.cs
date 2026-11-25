using Microsoft.AspNetCore.Mvc;
using Recruitment.Application.DTOs;
using Recruitment.Application.Interfaces;

namespace Recruitment.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuestionBanksController : ControllerBase
{
    private readonly IQuestionBankService _bankService;
    private readonly IOrganizationService _organizationService;

    public QuestionBanksController(IQuestionBankService bankService, IOrganizationService organizationService)
    {
        _bankService = bankService;
        _organizationService = organizationService;
    }

    [HttpGet("organization/{slug}")]
    public async Task<IActionResult> GetOrganizationBanks(string slug)
    {
        var org = await _organizationService.GetOrganizationAsync(slug);
        if (org == null) return NotFound(new { message = "Organization not found." });
        var banks = await _bankService.GetOrganizationBanksAsync(org.Id);
        return Ok(banks);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetBank(Guid id)
    {
        var bank = await _bankService.GetBankByIdAsync(id);
        if (bank == null) return NotFound(new { message = "Question bank not found." });
        return Ok(bank);
    }

    [HttpPost]
    public async Task<IActionResult> CreateBank(
        [FromQuery] string? organization,
        [FromQuery] string? organizationId,
        [FromQuery] Guid userId,
        [FromBody] CreateQuestionBankRequest request)
    {
        try
        {
            var orgKey = !string.IsNullOrWhiteSpace(organization) ? organization : organizationId;
            if (string.IsNullOrWhiteSpace(orgKey)) return BadRequest(new { message = "Organization identifier is required." });
            var org = await _organizationService.GetOrganizationAsync(orgKey);
            if (org == null) return NotFound(new { message = "Organization not found." });
            var created = await _bankService.CreateBankAsync(org.Id, userId, request);
            return CreatedAtAction(nameof(GetBank), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateBank(
        Guid id,
        [FromQuery] Guid userId,
        [FromBody] UpdateQuestionBankRequest request)
    {
        try
        {
            var updated = await _bankService.UpdateBankAsync(id, userId, request);
            return Ok(updated);
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

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteBank(Guid id, [FromQuery] Guid userId)
    {
        try
        {
            await _bankService.DeleteBankAsync(id, userId);
            return Ok(new { success = true });
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

    [HttpPost("{bankId:guid}/questions")]
    public async Task<IActionResult> AddQuestion(
        Guid bankId,
        [FromQuery] Guid userId,
        [FromBody] CreateQuestionRequest request)
    {
        try
        {
            var question = await _bankService.AddQuestionAsync(bankId, userId, request);
            return Ok(question);
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

    [HttpPut("questions/{questionId:guid}")]
    public async Task<IActionResult> UpdateQuestion(
        Guid questionId,
        [FromQuery] Guid userId,
        [FromBody] UpdateQuestionRequest request)
    {
        try
        {
            var updated = await _bankService.UpdateQuestionAsync(questionId, userId, request);
            return Ok(updated);
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

    [HttpDelete("questions/{questionId:guid}")]
    public async Task<IActionResult> DeleteQuestion(Guid questionId, [FromQuery] Guid userId)
    {
        try
        {
            await _bankService.DeleteQuestionAsync(questionId, userId);
            return Ok(new { success = true });
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

