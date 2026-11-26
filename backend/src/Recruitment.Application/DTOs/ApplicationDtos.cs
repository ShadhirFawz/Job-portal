using System.Text.Json.Serialization;

namespace Recruitment.Application.DTOs;

public class JobApplicantUserDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("avatar_path")]
    public string? AvatarPath { get; set; }

    [JsonPropertyName("headline")]
    public string? Headline { get; set; }

    [JsonPropertyName("current_job_title")]
    public string? CurrentJobTitle { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("country_code")]
    public string? CountryCode { get; set; }
}

public class JobApplicationResumeDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("file_name")]
    public string? FileName { get; set; }

    [JsonPropertyName("file_path")]
    public string? FilePath { get; set; }

    [JsonPropertyName("file_size")]
    public int? FileSize { get; set; }

    [JsonPropertyName("file_type")]
    public string? FileType { get; set; }
}

public class JobApplicationDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("job_id")]
    public Guid JobId { get; set; }

    [JsonPropertyName("user_id")]
    public Guid UserId { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("cover_letter")]
    public string? CoverLetter { get; set; }

    [JsonPropertyName("resume_id")]
    public Guid? ResumeId { get; set; }

    [JsonPropertyName("custom_answers")]
    public Dictionary<string, object>? CustomAnswers { get; set; }

    [JsonPropertyName("current_process_id")]
    public Guid? CurrentProcessId { get; set; }

    [JsonPropertyName("score")]
    public decimal? Score { get; set; }

    [JsonPropertyName("applied_at")]
    public DateTime AppliedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("withdrawn_at")]
    public DateTime? WithdrawnAt { get; set; }

    [JsonPropertyName("user")]
    public JobApplicantUserDto? User { get; set; }

    [JsonPropertyName("resume")]
    public JobApplicationResumeDto? Resume { get; set; }

    [JsonPropertyName("slug")]
    public string? Slug { get; set; }

    [JsonPropertyName("job")]
    public CandidateApplicationJobDto? Job { get; set; }

    [JsonPropertyName("selection_stages")]
    public List<CandidateSelectionStageDto>? SelectionStages { get; set; }

    [JsonPropertyName("quiz_attempts")]
    public List<CandidateQuizAttemptSummaryDto>? QuizAttempts { get; set; }
}

public class CandidateApplicationOrgDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string? Slug { get; set; }

    [JsonPropertyName("avatar_path")]
    public string? AvatarPath { get; set; }
}

public class CandidateApplicationJobDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string? Slug { get; set; }

    [JsonPropertyName("job_type")]
    public string JobType { get; set; } = string.Empty;

    [JsonPropertyName("work_mode")]
    public string WorkMode { get; set; } = string.Empty;

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("country_code")]
    public string? CountryCode { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("application_deadline")]
    public DateTime? ApplicationDeadline { get; set; }

    [JsonPropertyName("organization")]
    public CandidateApplicationOrgDto? Organization { get; set; }
}

public class CandidateSelectionStageDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string? Slug { get; set; }

    [JsonPropertyName("process_type")]
    public string ProcessType { get; set; } = string.Empty;

    [JsonPropertyName("order_index")]
    public int OrderIndex { get; set; }

    [JsonPropertyName("is_mandatory")]
    public bool IsMandatory { get; set; }

    [JsonPropertyName("time_limit_minutes")]
    public int? TimeLimitMinutes { get; set; }

    [JsonPropertyName("passing_score")]
    public decimal? PassingScore { get; set; }

    [JsonPropertyName("question_bank_id")]
    public Guid? QuestionBankId { get; set; }
}

public class CandidateQuizAttemptSummaryDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("selection_process_id")]
    public Guid SelectionProcessId { get; set; }

    [JsonPropertyName("application_id")]
    public Guid? ApplicationId { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("score")]
    public decimal Score { get; set; }

    [JsonPropertyName("max_score")]
    public decimal MaxScore { get; set; }

    [JsonPropertyName("percentage")]
    public decimal Percentage { get; set; }

    [JsonPropertyName("passed")]
    public bool? Passed { get; set; }

    [JsonPropertyName("requires_manual_review")]
    public bool RequiresManualReview { get; set; }

    [JsonPropertyName("started_at")]
    public DateTime StartedAt { get; set; }

    [JsonPropertyName("submitted_at")]
    public DateTime? SubmittedAt { get; set; }

    [JsonPropertyName("reviewed_at")]
    public DateTime? ReviewedAt { get; set; }

    [JsonPropertyName("passing_score")]
    public decimal? PassingScore { get; set; }

    [JsonPropertyName("time_limit_minutes")]
    public int? TimeLimitMinutes { get; set; }

    [JsonPropertyName("stage_title")]
    public string? StageTitle { get; set; }
}

public class CreateApplicationRequest
{
    [JsonPropertyName("job_id")]
    public Guid JobId { get; set; }

    [JsonPropertyName("user_id")]
    public Guid UserId { get; set; }

    [JsonPropertyName("cover_letter")]
    public string? CoverLetter { get; set; }

    [JsonPropertyName("resume_id")]
    public Guid? ResumeId { get; set; }

    [JsonPropertyName("custom_answers")]
    public Dictionary<string, object>? CustomAnswers { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; } = "applied";
}

public class UpdateApplicationStatusRequest
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}

