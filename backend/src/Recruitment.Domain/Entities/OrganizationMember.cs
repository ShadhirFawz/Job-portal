using System.Text.Json.Serialization;

namespace Recruitment.Domain.Entities;

public class OrganizationMember
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("organization_id")]
    public Guid OrganizationId { get; set; }

    [JsonPropertyName("user_id")]
    public Guid UserId { get; set; }

    [JsonPropertyName("role")]
    public string Role { get; set; } = "org_staff"; // org_admin, org_staff

    [JsonPropertyName("status")]
    public string Status { get; set; } = "pending"; // pending, active, revoked, rejected

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("department")]
    public string? Department { get; set; }

    [JsonPropertyName("joined_at")]
    public DateTime? JoinedAt { get; set; }

    [JsonPropertyName("invited_at")]
    public DateTime? InvitedAt { get; set; }

    [JsonPropertyName("invited_by")]
    public Guid? InvitedBy { get; set; }

    [JsonPropertyName("revoked_at")]
    public DateTime? RevokedAt { get; set; }

    [JsonPropertyName("revoked_by")]
    public Guid? RevokedBy { get; set; }

    [JsonPropertyName("rejection_reason")]
    public string? RejectionReason { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Joined relations
    [JsonPropertyName("user")]
    public UserProfile? User { get; set; }

    [JsonPropertyName("inviter")]
    public UserProfile? Inviter { get; set; }

    [JsonPropertyName("organization")]
    public Organization? Organization { get; set; }
}

