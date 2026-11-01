using System.Text.Json.Serialization;

namespace Recruitment.Domain.Entities;

public class JobApplication
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("job_id")]
    public Guid JobId { get; set; }

    [JsonPropertyName("user_id")]
    public Guid UserId { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "applied";

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
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("withdrawn_at")]
    public DateTime? WithdrawnAt { get; set; }

    // Joined relations
    [JsonPropertyName("user")]
    public UserProfile? User { get; set; }

    [JsonPropertyName("resume")]
    public Resume? Resume { get; set; }
}

