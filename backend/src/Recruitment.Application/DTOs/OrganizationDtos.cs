using System.Text.Json.Serialization;

namespace Recruitment.Application.DTOs;

public class OrganizationDto
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
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("verified_at")]
    public DateTime? VerifiedAt { get; set; }

    [JsonPropertyName("created_by")]
    public Guid CreatedBy { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

public class CreateOrganizationRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("legal_name")]
    public string? LegalName { get; set; }

    [JsonPropertyName("official_email")]
    public string OfficialEmail { get; set; } = string.Empty;

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
    public bool? HasPhysicalOffice { get; set; }

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
}

public class UpdateOrganizationRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("legal_name")]
    public string? LegalName { get; set; }

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
    public bool? HasPhysicalOffice { get; set; }

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
}

public class CreateEmailVerificationRequest
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("origin")]
    public string? Origin { get; set; }
}

public class EmailVerificationTokenResponse
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("verifyUrl")]
    public string VerifyUrl { get; set; } = string.Empty;
}

public class VerifyEmailRequest
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
}

public class VerifyEmailTokenResponse
{
    [JsonPropertyName("organizationId")]
    public Guid OrganizationId { get; set; }

    [JsonPropertyName("organizationName")]
    public string OrganizationName { get; set; } = string.Empty;

    [JsonPropertyName("officialEmail")]
    public string OfficialEmail { get; set; } = string.Empty;
}

