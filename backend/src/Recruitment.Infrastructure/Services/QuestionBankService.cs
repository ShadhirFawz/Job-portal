using Recruitment.Application.DTOs;
using Recruitment.Application.Interfaces;
using Recruitment.Domain.Entities;
using Recruitment.Infrastructure.Supabase;

namespace Recruitment.Infrastructure.Services;

public class QuestionBankService : IQuestionBankService
{
    private readonly ISupabaseContext _supabase;

    public QuestionBankService(ISupabaseContext supabase)
    {
        _supabase = supabase;
    }

    public async Task<List<QuestionBankDto>> GetOrganizationBanksAsync(Guid organizationId)
    {
        var banksQuery = $"question_banks?organization_id=eq.{organizationId}&deleted_at=is.null&order=created_at.desc" +
                         "&select=*,questions:questions(*),creator:users!question_banks_created_by_fkey(id,first_name,last_name,email)";
        var banks = await _supabase.GetListAsync<QuestionBank>(banksQuery);

        return banks.Select(MapBankToDto).ToList();
    }

    public async Task<QuestionBankDto?> GetBankByIdAsync(Guid bankId)
    {
        var query = $"question_banks?id=eq.{bankId}&deleted_at=is.null" +
                    "&select=*,questions:questions(*),creator:users!question_banks_created_by_fkey(id,first_name,last_name,email)";
        var bank = await _supabase.GetSingleAsync<QuestionBank>(query);
        return bank != null ? MapBankToDto(bank) : null;
    }

    public async Task<QuestionBankDto> CreateBankAsync(Guid organizationId, Guid userId, CreateQuestionBankRequest request)
    {
        var newBank = new
        {
            organization_id = organizationId,
            created_by = userId,
            name = request.Name.Trim(),
            description = request.Description?.Trim(),
            category = request.Category?.Trim(),
            is_active = true
        };

        var bank = await _supabase.InsertAsync<QuestionBank>("question_banks", newBank);

        // Optionally insert initial questions
        if (request.Questions != null && request.Questions.Count > 0)
        {
            var idx = 0;
            foreach (var q in request.Questions)
            {
                var newQ = new
                {
                    bank_id = bank.Id,
                    organization_id = organizationId,
                    created_by = userId,
                    body = q.Body.Trim(),
                    question_type = q.QuestionType,
                    options = q.Options ?? [],
                    correct_answer = q.CorrectAnswer?.Trim(),
                    explanation = q.Explanation?.Trim(),
                    difficulty = string.IsNullOrWhiteSpace(q.Difficulty) ? "medium" : q.Difficulty,
                    points = q.Points > 0 ? q.Points : 1,
                    order_index = idx++,
                    is_active = true
                };
                await _supabase.InsertAsync<Question>("questions", newQ);
            }
        }

        return (await GetBankByIdAsync(bank.Id))!;
    }

    public async Task<QuestionBankDto> UpdateBankAsync(Guid bankId, Guid userId, UpdateQuestionBankRequest request)
    {
        var bank = await _supabase.GetSingleAsync<QuestionBank>($"question_banks?id=eq.{bankId}&deleted_at=is.null");
        if (bank == null) throw new KeyNotFoundException("Question bank not found.");

        // Strict ownership check: only creator can modify!
        if (bank.CreatedBy != userId)
        {
            throw new UnauthorizedAccessException("Only the creator of this question bank is authorized to modify it.");
        }

        var updatePayload = new Dictionary<string, object>();
        if (!string.IsNullOrWhiteSpace(request.Name)) updatePayload["name"] = request.Name.Trim();
        if (request.Description != null) updatePayload["description"] = request.Description.Trim();
        if (request.Category != null) updatePayload["category"] = request.Category.Trim();
        updatePayload["updated_by"] = userId;
        updatePayload["updated_at"] = DateTime.UtcNow;

        await _supabase.UpdateAsync<QuestionBank>($"question_banks?id=eq.{bankId}", updatePayload);
        return (await GetBankByIdAsync(bankId))!;
    }

    public async Task DeleteBankAsync(Guid bankId, Guid userId)
    {
        var bank = await _supabase.GetSingleAsync<QuestionBank>($"question_banks?id=eq.{bankId}&deleted_at=is.null");
        if (bank == null) throw new KeyNotFoundException("Question bank not found.");

        // Strict ownership check: only creator can delete!
        if (bank.CreatedBy != userId)
        {
            throw new UnauthorizedAccessException("Only the creator of this question bank is authorized to delete it.");
        }

        await _supabase.UpdateAsync<QuestionBank>($"question_banks?id=eq.{bankId}", new
        {
            deleted_at = DateTime.UtcNow,
            updated_by = userId
        });
    }

    public async Task<QuestionDto> AddQuestionAsync(Guid bankId, Guid userId, CreateQuestionRequest request)
    {
        var bank = await _supabase.GetSingleAsync<QuestionBank>($"question_banks?id=eq.{bankId}&deleted_at=is.null");
        if (bank == null) throw new KeyNotFoundException("Question bank not found.");

        if (bank.CreatedBy != userId)
        {
            throw new UnauthorizedAccessException("Only the creator of this question bank is authorized to add questions.");
        }

        var newQ = new
        {
            bank_id = bankId,
            organization_id = bank.OrganizationId,
            created_by = userId,
            body = request.Body.Trim(),
            question_type = request.QuestionType,
            options = request.Options ?? [],
            correct_answer = request.CorrectAnswer?.Trim(),
            explanation = request.Explanation?.Trim(),
            difficulty = string.IsNullOrWhiteSpace(request.Difficulty) ? "medium" : request.Difficulty,
            points = request.Points > 0 ? request.Points : 1,
            order_index = request.OrderIndex,
            is_active = true
        };

        var created = await _supabase.InsertAsync<Question>("questions", newQ);
        return MapQuestionToDto(created);
    }

    public async Task<QuestionDto> UpdateQuestionAsync(Guid questionId, Guid userId, UpdateQuestionRequest request)
    {
        var question = await _supabase.GetSingleAsync<Question>($"questions?id=eq.{questionId}&deleted_at=is.null");
        if (question == null) throw new KeyNotFoundException("Question not found.");

        var bank = await _supabase.GetSingleAsync<QuestionBank>($"question_banks?id=eq.{question.BankId}&deleted_at=is.null");
        if (bank == null || bank.CreatedBy != userId)
        {
            throw new UnauthorizedAccessException("Only the creator of this question bank is authorized to modify its questions.");
        }

        var payload = new Dictionary<string, object>();
        if (!string.IsNullOrWhiteSpace(request.Body)) payload["body"] = request.Body.Trim();
        if (!string.IsNullOrWhiteSpace(request.QuestionType)) payload["question_type"] = request.QuestionType;
        if (request.Options != null) payload["options"] = request.Options;
        if (request.CorrectAnswer != null) payload["correct_answer"] = request.CorrectAnswer.Trim();
        if (request.Explanation != null) payload["explanation"] = request.Explanation.Trim();
        if (!string.IsNullOrWhiteSpace(request.Difficulty)) payload["difficulty"] = request.Difficulty;
        if (request.Points.HasValue) payload["points"] = request.Points.Value;
        if (request.OrderIndex.HasValue) payload["order_index"] = request.OrderIndex.Value;
        payload["updated_at"] = DateTime.UtcNow;

        var updated = await _supabase.UpdateAsync<Question>($"questions?id=eq.{questionId}", payload);
        return MapQuestionToDto(updated ?? question);
    }

    public async Task DeleteQuestionAsync(Guid questionId, Guid userId)
    {
        var question = await _supabase.GetSingleAsync<Question>($"questions?id=eq.{questionId}&deleted_at=is.null");
        if (question == null) throw new KeyNotFoundException("Question not found.");

        var bank = await _supabase.GetSingleAsync<QuestionBank>($"question_banks?id=eq.{question.BankId}&deleted_at=is.null");
        if (bank == null || bank.CreatedBy != userId)
        {
            throw new UnauthorizedAccessException("Only the creator of this question bank is authorized to delete questions.");
        }

        await _supabase.UpdateAsync<Question>($"questions?id=eq.{questionId}", new
        {
            deleted_at = DateTime.UtcNow
        });
    }

    private static QuestionBankDto MapBankToDto(QuestionBank b)
    {
        var activeQuestions = b.Questions?.Where(q => q.DeletedAt == null).OrderBy(q => q.OrderIndex).ToList() ?? [];
        var creatorName = b.Creator != null
            ? $"{b.Creator.FirstName} {b.Creator.LastName}".Trim()
            : null;
        if (string.IsNullOrWhiteSpace(creatorName) && b.Creator != null)
        {
            creatorName = b.Creator.Email;
        }

        return new QuestionBankDto
        {
            Id = b.Id,
            OrganizationId = b.OrganizationId,
            CreatedBy = b.CreatedBy,
            CreatorName = creatorName,
            Name = b.Name,
            Description = b.Description,
            Category = b.Category,
            QuestionsCount = activeQuestions.Count,
            TotalPoints = activeQuestions.Sum(q => q.Points),
            Questions = activeQuestions.Select(MapQuestionToDto).ToList(),
            CreatedAt = b.CreatedAt,
            UpdatedAt = b.UpdatedAt
        };
    }

    private static QuestionDto MapQuestionToDto(Question q)
    {
        return new QuestionDto
        {
            Id = q.Id,
            BankId = q.BankId,
            OrganizationId = q.OrganizationId,
            CreatedBy = q.CreatedBy,
            Body = q.Body,
            QuestionType = q.QuestionType,
            Options = q.Options?.Select(o => new QuestionOptionDto
            {
                Id = o.Id,
                Text = o.Text,
                IsCorrect = o.IsCorrect
            }).ToList(),
            CorrectAnswer = q.CorrectAnswer,
            Explanation = q.Explanation,
            Difficulty = q.Difficulty,
            Points = q.Points,
            OrderIndex = q.OrderIndex,
            IsActive = q.IsActive,
            CreatedAt = q.CreatedAt,
            UpdatedAt = q.UpdatedAt
        };
    }
}

