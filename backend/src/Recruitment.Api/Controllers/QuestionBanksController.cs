using Microsoft.AspNetCore.Mvc;
using Recruitment.Application.DTOs;
using Recruitment.Application.Interfaces;

namespace Recruitment.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuestionBanksController : ControllerBase
{
    private readonly IQuestionBankService _bankService;

    public QuestionBanksController(IQuestionBankService bankService)
    {
        _bankService = bankService;
    }

    [HttpGet("organization/{organizationId:guid}")]
    public async Task<IActionResult> GetOrganizationBanks(Guid organizationId)
    {
        var banks = await _bankService.GetOrganizationBanksAsync(organizationId);
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
        [FromQuery] Guid organizationId,
        [FromQuery] Guid userId,
        [FromBody] CreateQuestionBankRequest request)
    {
        try
        {
            var created = await _bankService.CreateBankAsync(organizationId, userId, request);
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

