using System.Text.Json.Serialization;

namespace Recruitment.Domain.Entities;

public class Organization
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("legal_name")]
    public string? LegalName { get; set; }

    [JsonPropertyName("official_email")]
    public string OfficialEmail { get; set; } = string.Empty;

    [JsonPropertyName("official_email_verified")]
    public bool OfficialEmailVerified { get; set; }

    [JsonPropertyName("tagline")]
    public string? Tagline { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("website_url")]
    public string? WebsiteUrl { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("industry")]
    public string? Industry { get; set; }

    [JsonPropertyName("company_size")]
    public string? CompanySize { get; set; }

    [JsonPropertyName("founded_year")]
    public int? FoundedYear { get; set; }

    [JsonPropertyName("company_type")]
    public string? CompanyType { get; set; }

    [JsonPropertyName("has_physical_office")]
    public bool HasPhysicalOffice { get; set; }

    [JsonPropertyName("country_code")]
    public string? CountryCode { get; set; }

    [JsonPropertyName("state_province")]
    public string? StateProvince { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("postal_code")]
    public string? PostalCode { get; set; }

    [JsonPropertyName("address_line1")]
    public string? AddressLine1 { get; set; }

    [JsonPropertyName("address_line2")]
    public string? AddressLine2 { get; set; }

    [JsonPropertyName("latitude")]
    public decimal? Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public decimal? Longitude { get; set; }

    [JsonPropertyName("avatar_path")]
    public string? AvatarPath { get; set; }

    [JsonPropertyName("cover_path")]
    public string? CoverPath { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "pending_verification";

    [JsonPropertyName("verified_at")]
    public DateTime? VerifiedAt { get; set; }

    [JsonPropertyName("created_by")]
    public Guid CreatedBy { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

