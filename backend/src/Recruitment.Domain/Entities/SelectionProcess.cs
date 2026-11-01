using System.Text.Json.Serialization;

namespace Recruitment.Domain.Entities;

public class SelectionProcess
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("job_id")]
    public Guid JobId { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("process_type")]
    public string ProcessType { get; set; } = "quiz";

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("order_index")]
    public int OrderIndex { get; set; }

    [JsonPropertyName("is_mandatory")]
    public bool IsMandatory { get; set; } = true;

    [JsonPropertyName("time_limit_minutes")]
    public int? TimeLimitMinutes { get; set; }

    [JsonPropertyName("passing_score")]
    public int? PassingScore { get; set; }

    [JsonPropertyName("config")]
    public Dictionary<string, object>? Config { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

