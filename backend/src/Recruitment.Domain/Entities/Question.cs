using System.Text.Json.Serialization;

namespace Recruitment.Domain.Entities;

public class QuestionOption
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("is_correct")]
    public bool IsCorrect { get; set; }
}

public class Question
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("bank_id")]
    public Guid BankId { get; set; }

    [JsonPropertyName("organization_id")]
    public Guid OrganizationId { get; set; }

    [JsonPropertyName("created_by")]
    public Guid CreatedBy { get; set; }

    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;

    [JsonPropertyName("question_type")]
    public string QuestionType { get; set; } = "single_choice"; // single_choice, multiple_choice, true_false, structured

    [JsonPropertyName("options")]
    public List<QuestionOption>? Options { get; set; }

    [JsonPropertyName("correct_answer")]
    public string? CorrectAnswer { get; set; }

    [JsonPropertyName("explanation")]
    public string? Explanation { get; set; }

    [JsonPropertyName("difficulty")]
    public string Difficulty { get; set; } = "medium"; // easy, medium, hard

    [JsonPropertyName("points")]
    public decimal Points { get; set; } = 1;

    [JsonPropertyName("order_index")]
    public int OrderIndex { get; set; }

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; } = true;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("deleted_at")]
    public DateTime? DeletedAt { get; set; }
}

