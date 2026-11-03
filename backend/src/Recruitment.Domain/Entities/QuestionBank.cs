using System.Text.Json.Serialization;

namespace Recruitment.Domain.Entities;

public class QuestionBank
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("organization_id")]
    public Guid OrganizationId { get; set; }

    [JsonPropertyName("created_by")]
    public Guid CreatedBy { get; set; }

    [JsonPropertyName("updated_by")]
    public Guid? UpdatedBy { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; } = true;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    // Navigation / join helpers
    [JsonPropertyName("questions")]
    public List<Question>? Questions { get; set; }

    [JsonPropertyName("creator")]
    public UserProfile? Creator { get; set; }
}

