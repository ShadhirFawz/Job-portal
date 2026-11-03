using System.Text.Json;
using Recruitment.Application.DTOs;
using Recruitment.Application.Interfaces;
using Recruitment.Domain.Entities;
using Recruitment.Infrastructure.Supabase;

namespace Recruitment.Infrastructure.Services;

public class AssessmentService : IAssessmentService
{
    private readonly ISupabaseContext _supabase;

    public AssessmentService(ISupabaseContext supabase)
    {
        _supabase = supabase;
    }

    public async Task<QuizAttemptDto> StartAssessmentAsync(Guid userId, StartAssessmentRequest request)
    {
        // 1. Fetch selection process
        var process = await _supabase.GetSingleAsync<SelectionProcess>(
            $"job_selection_processes?id=eq.{request.SelectionProcessId}");
        if (process == null) throw new KeyNotFoundException("Selection process stage not found.");

        // 2. Fetch Job title
        var job = await _supabase.GetSingleAsync<Job>($"jobs?id=eq.{request.JobId}&select=id,title");

        // 3. Determine Question Bank ID
        Guid? bankId = process.QuestionBankId;
        if (!bankId.HasValue && process.Config != null &&
            process.Config.TryGetValue("question_bank_id", out var cfgBankId) &&
            Guid.TryParse(cfgBankId?.ToString(), out var parsedId))
        {
            bankId = parsedId;
        }

        if (!bankId.HasValue)
        {
            throw new InvalidOperationException("This assessment stage does not have a question bank assigned.");
        }

        // 4. Check for existing active attempt
        var existingAttempt = await _supabase.GetSingleAsync<QuizAttempt>(
            $"quiz_attempts?selection_process_id=eq.{request.SelectionProcessId}&user_id=eq.{userId}&status=eq.in_progress&order=started_at.desc");

        QuizAttempt attempt;
        if (existingAttempt != null)
        {
            // If expired, mark as expired and create new if allowed, else reuse
            if (existingAttempt.ExpiresAt.HasValue && existingAttempt.ExpiresAt.Value < DateTime.UtcNow)
            {
                await _supabase.UpdateAsync<QuizAttempt>($"quiz_attempts?id=eq.{existingAttempt.Id}", new
                {
                    status = "expired",
                    updated_at = DateTime.UtcNow
                });
                attempt = await CreateNewAttemptAsync(userId, request, process, bankId.Value);
            }
            else
            {
                attempt = existingAttempt;
            }
        }
        else
        {
            attempt = await CreateNewAttemptAsync(userId, request, process, bankId.Value);
        }

        // 5. Fetch questions for the bank
        var questions = await _supabase.GetListAsync<Question>(
            $"questions?bank_id=eq.{bankId.Value}&deleted_at=is.null&order=order_index.asc");

        return MapAttemptToDto(attempt, process, job?.Title, questions);
    }

    private async Task<QuizAttempt> CreateNewAttemptAsync(
        Guid userId, StartAssessmentRequest request, SelectionProcess process, Guid bankId)
    {
        DateTime? expiresAt = null;
        if (process.TimeLimitMinutes.HasValue && process.TimeLimitMinutes.Value > 0)
        {
            expiresAt = DateTime.UtcNow.AddMinutes(process.TimeLimitMinutes.Value);
        }

        var newAttempt = new
        {
            job_id = request.JobId,
            selection_process_id = request.SelectionProcessId,
            application_id = request.ApplicationId,
            bank_id = bankId,
            user_id = userId,
            status = "in_progress",
            started_at = DateTime.UtcNow,
            expires_at = expiresAt,
            score = 0,
            max_score = 0,
            percentage = 0
        };

        return await _supabase.InsertAsync<QuizAttempt>("quiz_attempts", newAttempt);
    }

    public async Task<QuizAttemptDto?> GetAttemptByIdAsync(Guid attemptId, Guid userId)
    {
        var attempt = await _supabase.GetSingleAsync<QuizAttempt>($"quiz_attempts?id=eq.{attemptId}");
        if (attempt == null) return null;

        var process = await _supabase.GetSingleAsync<SelectionProcess>($"job_selection_processes?id=eq.{attempt.SelectionProcessId}");
        var job = await _supabase.GetSingleAsync<Job>($"jobs?id=eq.{attempt.JobId}&select=id,title");

        List<Question> questions = [];
        if (attempt.BankId.HasValue)
        {
            questions = await _supabase.GetListAsync<Question>(
                $"questions?bank_id=eq.{attempt.BankId.Value}&deleted_at=is.null&order=order_index.asc");
        }

        var answers = await _supabase.GetListAsync<QuizAnswer>($"quiz_answers?attempt_id=eq.{attemptId}&select=*,question:questions(*)");

        var dto = MapAttemptToDto(attempt, process, job?.Title, questions);
        dto.Answers = answers.Select(a => new QuizAnswerDetailDto
        {
            QuestionId = a.QuestionId,
            QuestionBody = a.Question?.Body ?? string.Empty,
            QuestionType = a.Question?.QuestionType ?? string.Empty,
            Answer = a.Answer,
            IsCorrect = a.IsCorrect,
            PointsAwarded = a.PointsAwarded,
            MaxPoints = a.Question?.Points ?? 1,
            Explanation = a.Question?.Explanation
        }).ToList();

        return dto;
    }

    public async Task<QuizAttemptDto?> GetUserStageAttemptAsync(Guid selectionProcessId, Guid userId)
    {
        var attempt = await _supabase.GetSingleAsync<QuizAttempt>(
            $"quiz_attempts?selection_process_id=eq.{selectionProcessId}&user_id=eq.{userId}&order=created_at.desc");
        if (attempt == null) return null;

        return await GetAttemptByIdAsync(attempt.Id, userId);
    }

    public async Task<QuizAttemptDto> SubmitAssessmentAsync(Guid userId, SubmitAssessmentRequest request)
    {
        var attempt = await _supabase.GetSingleAsync<QuizAttempt>($"quiz_attempts?id=eq.{request.AttemptId}");
        if (attempt == null) throw new KeyNotFoundException("Assessment attempt not found.");

        if (attempt.UserId != userId)
        {
            throw new UnauthorizedAccessException("You are not authorized to submit this assessment.");
        }

        if (attempt.Status == "submitted" || attempt.Status == "graded")
        {
            return (await GetAttemptByIdAsync(attempt.Id, userId))!;
        }

        // Fetch selection process & questions
        var process = await _supabase.GetSingleAsync<SelectionProcess>($"job_selection_processes?id=eq.{attempt.SelectionProcessId}");
        var questions = attempt.BankId.HasValue
            ? await _supabase.GetListAsync<Question>($"questions?bank_id=eq.{attempt.BankId.Value}&deleted_at=is.null")
            : [];

        var questionMap = questions.ToDictionary(q => q.Id);

        decimal totalScore = 0;
        decimal maxScore = questions.Sum(q => q.Points);

        var answerRows = new List<object>();

        foreach (var answerItem in request.Answers)
        {
            if (!questionMap.TryGetValue(answerItem.QuestionId, out var q)) continue;

            bool? isCorrect = null;
            decimal pointsAwarded = 0;

            var rawAns = answerItem.Answer;
            var ansStr = rawAns?.ToString()?.Trim() ?? string.Empty;

            if (q.QuestionType == "single_choice" || q.QuestionType == "true_false")
            {
                var correctOption = q.Options?.FirstOrDefault(o => o.IsCorrect);
                if (correctOption != null && string.Equals(ansStr, correctOption.Id, StringComparison.OrdinalIgnoreCase))
                {
                    isCorrect = true;
                    pointsAwarded = q.Points;
                }
                else
                {
                    isCorrect = false;
                }
            }
            else if (q.QuestionType == "multiple_choice")
            {
                var correctIds = (q.Options?.Where(o => o.IsCorrect).Select(o => o.Id).OrderBy(x => x).ToList()) ?? [];

                List<string> candidateSelected = [];
                if (rawAns is JsonElement elem && elem.ValueKind == JsonValueKind.Array)
                {
                    candidateSelected = elem.EnumerateArray()
                        .Select(e => e.GetString() ?? string.Empty)
                        .Where(s => !string.IsNullOrEmpty(s))
                        .OrderBy(x => x)
                        .ToList();
                }
                else if (rawAns is IEnumerable<string> strEnum)
                {
                    candidateSelected = strEnum.OrderBy(x => x).ToList();
                }

                if (correctIds.SequenceEqual(candidateSelected, StringComparer.OrdinalIgnoreCase))
                {
                    isCorrect = true;
                    pointsAwarded = q.Points;
                }
                else
                {
                    isCorrect = false;
                }
            }
            else if (q.QuestionType == "structured")
            {
                // Structured question: open-ended written text
                // Does not auto-grade — leave isCorrect as null, pending manual evaluation
                isCorrect = null;
                pointsAwarded = 0;
            }

            totalScore += pointsAwarded;

            answerRows.Add(new
            {
                attempt_id = attempt.Id,
                question_id = q.Id,
                answer = rawAns,
                is_correct = isCorrect,
                points_awarded = pointsAwarded,
                answered_at = DateTime.UtcNow
            });
        }

        // Insert answers
        foreach (var row in answerRows)
        {
            try
            {
                await _supabase.InsertAsync<QuizAnswer>("quiz_answers", row);
            }
            catch
            {
                // Ignore unique constraint collision if re-submitting an answer
            }
        }

        var percentage = maxScore > 0 ? Math.Round((totalScore / maxScore) * 100, 2) : 0;
        var passingScore = process?.PassingScore ?? 70;
        var passed = percentage >= passingScore;

        var hasStructured = questions.Any(q => q.QuestionType == "structured");
        var finalStatus = hasStructured ? "submitted" : "graded";

        await _supabase.UpdateAsync<QuizAttempt>($"quiz_attempts?id=eq.{attempt.Id}", new
        {
            status = finalStatus,
            submitted_at = DateTime.UtcNow,
            score = totalScore,
            max_score = maxScore,
            percentage = percentage,
            passed = passed,
            updated_at = DateTime.UtcNow
        });

        // If application is linked, update score on job_applications
        if (attempt.ApplicationId.HasValue)
        {
            try
            {
                await _supabase.UpdateAsync<JobApplication>($"job_applications?id=eq.{attempt.ApplicationId.Value}", new
                {
                    score = percentage,
                    current_process_id = attempt.SelectionProcessId,
                    updated_at = DateTime.UtcNow
                });
            }
            catch
            {
                // non-fatal
            }
        }

        return (await GetAttemptByIdAsync(attempt.Id, userId))!;
    }

    private static QuizAttemptDto MapAttemptToDto(
        QuizAttempt a, SelectionProcess? process, string? jobTitle, List<Question> questions)
    {
        return new QuizAttemptDto
        {
            Id = a.Id,
            JobId = a.JobId,
            SelectionProcessId = a.SelectionProcessId,
            ApplicationId = a.ApplicationId,
            BankId = a.BankId,
            UserId = a.UserId,
            Status = a.Status,
            StartedAt = a.StartedAt,
            SubmittedAt = a.SubmittedAt,
            ExpiresAt = a.ExpiresAt,
            TimeLimitMinutes = process?.TimeLimitMinutes,
            Score = a.Score,
            MaxScore = a.MaxScore,
            Percentage = a.Percentage,
            PassingScore = process?.PassingScore,
            Passed = a.Passed,
            StageTitle = process?.Title ?? "Quiz Assessment",
            JobTitle = jobTitle,
            Questions = questions.Select(q => new CandidateQuestionDto
            {
                Id = q.Id,
                Body = q.Body,
                QuestionType = q.QuestionType,
                Points = q.Points,
                OrderIndex = q.OrderIndex,
                // CRUCIAL: Map options without is_correct to prevent answer leakage!
                Options = q.Options?.Select(o => new CandidateQuestionOptionDto
                {
                    Id = o.Id,
                    Text = o.Text
                }).ToList()
            }).ToList()
        };
    }
}

