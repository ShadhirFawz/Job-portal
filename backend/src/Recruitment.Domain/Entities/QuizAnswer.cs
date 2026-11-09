using System.Text.Json.Serialization;

namespace Recruitment.Domain.Entities;

public class QuizAnswer
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("attempt_id")]
    public Guid AttemptId { get; set; }

    [JsonPropertyName("question_id")]
    public Guid QuestionId { get; set; }

    // Can store string option ID, array of option IDs, or written text
    [JsonPropertyName("answer")]
    public object? Answer { get; set; }

    [JsonPropertyName("is_correct")]
    public bool? IsCorrect { get; set; }

    [JsonPropertyName("points_awarded")]
    public decimal PointsAwarded { get; set; }

    [JsonPropertyName("answered_at")]
    public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("question")]
    public Question? Question { get; set; }
}

