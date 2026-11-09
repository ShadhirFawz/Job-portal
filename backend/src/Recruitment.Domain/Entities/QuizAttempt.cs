using System.Text.Json.Serialization;

namespace Recruitment.Domain.Entities;

public class QuizAttempt
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
    public string Status { get; set; } = "in_progress"; // in_progress, submitted, graded, expired

    [JsonPropertyName("started_at")]
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("submitted_at")]
    public DateTime? SubmittedAt { get; set; }

    [JsonPropertyName("expires_at")]
    public DateTime? ExpiresAt { get; set; }

    [JsonPropertyName("score")]
    public decimal Score { get; set; }

    [JsonPropertyName("max_score")]
    public decimal MaxScore { get; set; }

    [JsonPropertyName("percentage")]
    public decimal Percentage { get; set; }

    [JsonPropertyName("passed")]
    public bool? Passed { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation / join helpers
    [JsonPropertyName("answers")]
    public List<QuizAnswer>? Answers { get; set; }

    [JsonPropertyName("bank")]
    public QuestionBank? Bank { get; set; }

    [JsonPropertyName("selection_process")]
    public SelectionProcess? SelectionProcess { get; set; }
}

