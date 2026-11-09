using System.Text.Json.Serialization;

namespace Recruitment.Application.DTOs;

public class UserProfileDto
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

    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("avatar_path")]
    public string? AvatarPath { get; set; }

    [JsonPropertyName("cover_path")]
    public string? CoverPath { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("headline")]
    public string? Headline { get; set; }

    [JsonPropertyName("current_job_title")]
    public string? CurrentJobTitle { get; set; }

    [JsonPropertyName("current_company")]
    public string? CurrentCompany { get; set; }

    [JsonPropertyName("bio")]
    public string? Bio { get; set; }

    [JsonPropertyName("website_url")]
    public string? WebsiteUrl { get; set; }

    [JsonPropertyName("linkedin_url")]
    public string? LinkedinUrl { get; set; }

    [JsonPropertyName("github_url")]
    public string? GithubUrl { get; set; }

    [JsonPropertyName("portfolio_url")]
    public string? PortfolioUrl { get; set; }

    [JsonPropertyName("country_code")]
    public string? CountryCode { get; set; }

    [JsonPropertyName("state_province")]
    public string? StateProvince { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("years_of_experience")]
    public decimal? YearsOfExperience { get; set; }

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("is_profile_complete")]
    public bool IsProfileComplete { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

public class UpdateProfileRequest
{
    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("avatar_path")]
    public string? AvatarPath { get; set; }

    [JsonPropertyName("cover_path")]
    public string? CoverPath { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("headline")]
    public string? Headline { get; set; }

    [JsonPropertyName("current_job_title")]
    public string? CurrentJobTitle { get; set; }

    [JsonPropertyName("current_company")]
    public string? CurrentCompany { get; set; }

    [JsonPropertyName("bio")]
    public string? Bio { get; set; }

    [JsonPropertyName("website_url")]
    public string? WebsiteUrl { get; set; }

    [JsonPropertyName("linkedin_url")]
    public string? LinkedinUrl { get; set; }

    [JsonPropertyName("github_url")]
    public string? GithubUrl { get; set; }

    [JsonPropertyName("portfolio_url")]
    public string? PortfolioUrl { get; set; }

    [JsonPropertyName("country_code")]
    public string? CountryCode { get; set; }

    [JsonPropertyName("state_province")]
    public string? StateProvince { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("years_of_experience")]
    public decimal? YearsOfExperience { get; set; }

    [JsonPropertyName("is_profile_complete")]
    public bool? IsProfileComplete { get; set; }
}

public class UsernameCheckResultDto
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("is_available")]
    public bool IsAvailable { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

