using System.Text.Json.Serialization;

namespace Recruitment.Application.DTOs;

public class ResumeDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("user_id")]
    public Guid UserId { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("is_primary")]
    public bool IsPrimary { get; set; }

    [JsonPropertyName("file_path")]
    public string? FilePath { get; set; }

    [JsonPropertyName("file_name")]
    public string? FileName { get; set; }

    [JsonPropertyName("file_size")]
    public int? FileSize { get; set; }

    [JsonPropertyName("file_type")]
    public string? FileType { get; set; }

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("skills")]
    public string[]? Skills { get; set; }

    [JsonPropertyName("is_public")]
    public bool IsPublic { get; set; }

    [JsonPropertyName("last_used_at")]
    public DateTime? LastUsedAt { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("deleted_at")]
    public DateTime? DeletedAt { get; set; }
}

public class CreateResumeRequest
{
    [JsonPropertyName("user_id")]
    public Guid UserId { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("file_path")]
    public string FilePath { get; set; } = string.Empty;

    [JsonPropertyName("file_name")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("file_size")]
    public int FileSize { get; set; }

    [JsonPropertyName("file_type")]
    public string FileType { get; set; } = string.Empty;

    [JsonPropertyName("is_primary")]
    public bool? IsPrimary { get; set; }
}

public class UpdateResumeRequest
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("is_primary")]
    public bool? IsPrimary { get; set; }

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("skills")]
    public string[]? Skills { get; set; }
}

public class SignedUrlResponse
{
    [JsonPropertyName("signedUrl")]
    public string? SignedUrl { get; set; }
}

