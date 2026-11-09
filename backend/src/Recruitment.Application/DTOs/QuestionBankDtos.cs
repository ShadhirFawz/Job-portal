using System.Text.Json.Serialization;

namespace Recruitment.Application.DTOs;

public class QuestionOptionDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("is_correct")]
    public bool IsCorrect { get; set; }
}

public class QuestionDto
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
    public string QuestionType { get; set; } = "single_choice";

    [JsonPropertyName("options")]
    public List<QuestionOptionDto>? Options { get; set; }

    [JsonPropertyName("correct_answer")]
    public string? CorrectAnswer { get; set; }

    [JsonPropertyName("explanation")]
    public string? Explanation { get; set; }

    [JsonPropertyName("difficulty")]
    public string Difficulty { get; set; } = "medium";

    [JsonPropertyName("points")]
    public decimal Points { get; set; } = 1;

    [JsonPropertyName("order_index")]
    public int OrderIndex { get; set; }

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; } = true;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

public class QuestionBankDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("organization_id")]
    public Guid OrganizationId { get; set; }

    [JsonPropertyName("created_by")]
    public Guid CreatedBy { get; set; }

    [JsonPropertyName("creator_name")]
    public string? CreatorName { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("questions_count")]
    public int QuestionsCount { get; set; }

    [JsonPropertyName("total_points")]
    public decimal TotalPoints { get; set; }

    [JsonPropertyName("questions")]
    public List<QuestionDto>? Questions { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

public class CreateQuestionBankRequest
{
    [JsonPropertyName("organization_id")]
    public Guid OrganizationId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("questions")]
    public List<CreateQuestionRequest>? Questions { get; set; }
}

public class UpdateQuestionBankRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }
}

public class CreateQuestionRequest
{
    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;

    [JsonPropertyName("question_type")]
    public string QuestionType { get; set; } = "single_choice";

    [JsonPropertyName("options")]
    public List<QuestionOptionDto>? Options { get; set; }

    [JsonPropertyName("correct_answer")]
    public string? CorrectAnswer { get; set; }

    [JsonPropertyName("explanation")]
    public string? Explanation { get; set; }

    [JsonPropertyName("difficulty")]
    public string Difficulty { get; set; } = "medium";

    [JsonPropertyName("points")]
    public decimal Points { get; set; } = 1;

    [JsonPropertyName("order_index")]
    public int OrderIndex { get; set; }
}

public class UpdateQuestionRequest
{
    [JsonPropertyName("body")]
    public string? Body { get; set; }

    [JsonPropertyName("question_type")]
    public string? QuestionType { get; set; }

    [JsonPropertyName("options")]
    public List<QuestionOptionDto>? Options { get; set; }

    [JsonPropertyName("correct_answer")]
    public string? CorrectAnswer { get; set; }

    [JsonPropertyName("explanation")]
    public string? Explanation { get; set; }

    [JsonPropertyName("difficulty")]
    public string? Difficulty { get; set; }

    [JsonPropertyName("points")]
    public decimal? Points { get; set; }

    [JsonPropertyName("order_index")]
    public int? OrderIndex { get; set; }
}

