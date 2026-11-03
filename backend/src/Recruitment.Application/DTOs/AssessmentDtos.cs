using System.Text.Json.Serialization;

namespace Recruitment.Application.DTOs;

public class CandidateQuestionOptionDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

public class CandidateQuestionDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;

    [JsonPropertyName("question_type")]
    public string QuestionType { get; set; } = "single_choice";

    [JsonPropertyName("options")]
    public List<CandidateQuestionOptionDto>? Options { get; set; }

    [JsonPropertyName("points")]
    public decimal Points { get; set; } = 1;

    [JsonPropertyName("order_index")]
    public int OrderIndex { get; set; }
}

public class StartAssessmentRequest
{
    [JsonPropertyName("job_id")]
    public Guid JobId { get; set; }

    [JsonPropertyName("selection_process_id")]
    public Guid SelectionProcessId { get; set; }

    [JsonPropertyName("application_id")]
    public Guid? ApplicationId { get; set; }
}

public class SubmitAnswerItem
{
    [JsonPropertyName("question_id")]
    public Guid QuestionId { get; set; }

    // String (for single, true_false, structured) or array of strings (for multiple_choice)
    [JsonPropertyName("answer")]
    public object? Answer { get; set; }
}

public class SubmitAssessmentRequest
{
    [JsonPropertyName("attempt_id")]
    public Guid AttemptId { get; set; }

    [JsonPropertyName("answers")]
    public List<SubmitAnswerItem> Answers { get; set; } = [];
}

public class QuizAnswerDetailDto
{
    [JsonPropertyName("question_id")]
    public Guid QuestionId { get; set; }

    [JsonPropertyName("question_body")]
    public string QuestionBody { get; set; } = string.Empty;

    [JsonPropertyName("question_type")]
    public string QuestionType { get; set; } = string.Empty;

    [JsonPropertyName("answer")]
    public object? Answer { get; set; }

    [JsonPropertyName("is_correct")]
    public bool? IsCorrect { get; set; }

    [JsonPropertyName("points_awarded")]
    public decimal PointsAwarded { get; set; }

    [JsonPropertyName("max_points")]
    public decimal MaxPoints { get; set; }

    [JsonPropertyName("explanation")]
    public string? Explanation { get; set; }
}

public class QuizAttemptDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("job_id")]
    public Guid JobId { get; set; }

    [JsonPropertyName("selection_process_id")]
    public Guid SelectionProcessId { get; set; }

    [JsonPropertyName("application_id")]
    public Guid? ApplicationId { get; set; }

    [JsonPropertyName("bank_id")]
    public Guid? BankId { get; set; }

    [JsonPropertyName("user_id")]
    public Guid UserId { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "in_progress";

    [JsonPropertyName("started_at")]
    public DateTime StartedAt { get; set; }

    [JsonPropertyName("submitted_at")]
    public DateTime? SubmittedAt { get; set; }

    [JsonPropertyName("expires_at")]
    public DateTime? ExpiresAt { get; set; }

    [JsonPropertyName("time_limit_minutes")]
    public int? TimeLimitMinutes { get; set; }

    [JsonPropertyName("score")]
    public decimal Score { get; set; }

    [JsonPropertyName("max_score")]
    public decimal MaxScore { get; set; }

    [JsonPropertyName("percentage")]
    public decimal Percentage { get; set; }

    [JsonPropertyName("passing_score")]
    public int? PassingScore { get; set; }

    [JsonPropertyName("passed")]
    public bool? Passed { get; set; }

    [JsonPropertyName("stage_title")]
    public string? StageTitle { get; set; }

    [JsonPropertyName("job_title")]
    public string? JobTitle { get; set; }

    [JsonPropertyName("questions")]
    public List<CandidateQuestionDto>? Questions { get; set; }

    [JsonPropertyName("answers")]
    public List<QuizAnswerDetailDto>? Answers { get; set; }
}

