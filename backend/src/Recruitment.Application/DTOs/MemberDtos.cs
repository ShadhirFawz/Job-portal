using System.Text.Json.Serialization;

namespace Recruitment.Application.DTOs;

public class OrgMemberUserDto
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
}

public class OrgMemberDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("organization_id")]
    public Guid OrganizationId { get; set; }

    [JsonPropertyName("user_id")]
    public Guid UserId { get; set; }

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

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

    [JsonPropertyName("user")]
    public OrgMemberUserDto? User { get; set; }

    [JsonPropertyName("inviter")]
    public OrgMemberUserDto? Inviter { get; set; }
}

public class InviteMemberRequest
{
    [JsonPropertyName("organizationId")]
    public Guid OrganizationId { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = "org_staff";

    [JsonPropertyName("title")]
    public string? Title { get; set; }
}

public class UpdateMemberRoleRequest
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "org_staff";
}

public class UpdateMemberDetailsRequest
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("department")]
    public string? Department { get; set; }
}

public class RejectMemberRequest
{
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}

public class UserInvitationOrgDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("tagline")]
    public string? Tagline { get; set; }

    [JsonPropertyName("avatar_path")]
    public string? AvatarPath { get; set; }

    [JsonPropertyName("industry")]
    public string? Industry { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }
}

public class UserInvitationItemDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("invited_at")]
    public DateTime? InvitedAt { get; set; }

    [JsonPropertyName("organization")]
    public UserInvitationOrgDto? Organization { get; set; }
}

