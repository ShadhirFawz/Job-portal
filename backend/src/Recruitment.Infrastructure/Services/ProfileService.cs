using Recruitment.Application.DTOs;
using Recruitment.Application.Interfaces;
using Recruitment.Domain.Entities;
using Recruitment.Infrastructure.Supabase;

namespace Recruitment.Infrastructure.Services;

public class ProfileService : IProfileService
{
    private readonly ISupabaseContext _supabase;

    public ProfileService(ISupabaseContext supabase)
    {
        _supabase = supabase;
    }

    private static readonly HashSet<string> ReservedUsernames = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin", "administrator", "api", "auth", "login", "register", "logout",
        "profile", "profiles", "user", "users", "organization", "organizations",
        "job", "jobs", "resume", "resumes", "assessment", "assessments", "invitation", "invitations",
        "setting", "settings", "support", "help", "about", "terms", "privacy", "dashboard", "home", "u"
    };

    public async Task<UserProfileDto?> GetUserProfileAsync(Guid userId)
    {
        if (userId == Guid.Empty) return null;

        var profile = await _supabase.GetSingleAsync<UserProfile>($"users?id=eq.{userId}&select=*");
        return profile != null ? MapToDto(profile) : null;
    }

    public async Task<UserProfileDto?> GetUserProfileByUsernameAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username)) return null;
        var cleanUsername = username.Trim().ToLowerInvariant();
        var profile = await _supabase.GetSingleAsync<UserProfile>($"users?username=ilike.{cleanUsername}&select=*");
        return profile != null ? MapToDto(profile) : null;
    }

    public async Task<UsernameCheckResultDto> CheckUsernameAvailabilityAsync(string username, Guid? currentUserId = null)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return new UsernameCheckResultDto
            {
                Username = username ?? string.Empty,
                IsAvailable = false,
                Message = "Username is required."
            };
        }

        var clean = username.Trim().ToLowerInvariant();

        if (clean.Length < 3 || clean.Length > 30)
        {
            return new UsernameCheckResultDto
            {
                Username = clean,
                IsAvailable = false,
                Message = "Username must be between 3 and 30 characters."
            };
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(clean, "^[a-z0-9_]+$"))
        {
            return new UsernameCheckResultDto
            {
                Username = clean,
                IsAvailable = false,
                Message = "Username can only contain letters, numbers, and underscores."
            };
        }

        if (ReservedUsernames.Contains(clean))
        {
            return new UsernameCheckResultDto
            {
                Username = clean,
                IsAvailable = false,
                Message = "This username is reserved and cannot be used."
            };
        }

        var existing = await _supabase.GetSingleAsync<UserProfile>($"users?username=ilike.{clean}&select=id,username");
        if (existing != null)
        {
            if (currentUserId.HasValue && existing.Id == currentUserId.Value)
            {
                return new UsernameCheckResultDto
                {
                    Username = clean,
                    IsAvailable = true,
                    Message = "This is your current username."
                };
            }

            return new UsernameCheckResultDto
            {
                Username = clean,
                IsAvailable = false,
                Message = "Username is already taken."
            };
        }

        return new UsernameCheckResultDto
        {
            Username = clean,
            IsAvailable = true,
            Message = "Username is available!"
        };
    }

    public async Task<UserProfileDto> UpdateUserProfileAsync(Guid userId, UpdateProfileRequest request)
    {
        var payload = new Dictionary<string, object?>();

        if (request.FirstName != null) payload["first_name"] = request.FirstName.Trim();
        if (request.LastName != null) payload["last_name"] = request.LastName.Trim();
        if (request.DisplayName != null) payload["display_name"] = request.DisplayName.Trim();

        if (request.Username != null)
        {
            var check = await CheckUsernameAvailabilityAsync(request.Username, userId);
            if (!check.IsAvailable)
            {
                throw new InvalidOperationException(check.Message);
            }
            payload["username"] = request.Username.Trim().ToLowerInvariant();
        }

        if (request.AvatarPath != null) payload["avatar_path"] = request.AvatarPath;
        if (request.CoverPath != null) payload["cover_path"] = request.CoverPath;
        if (request.Phone != null) payload["phone"] = request.Phone.Trim();
        if (request.Headline != null) payload["headline"] = request.Headline.Trim();
        if (request.CurrentJobTitle != null) payload["current_job_title"] = request.CurrentJobTitle.Trim();
        if (request.CurrentCompany != null) payload["current_company"] = request.CurrentCompany.Trim();
        if (request.Bio != null) payload["bio"] = request.Bio.Trim();
        if (request.WebsiteUrl != null) payload["website_url"] = request.WebsiteUrl.Trim();
        if (request.LinkedinUrl != null) payload["linkedin_url"] = request.LinkedinUrl.Trim();
        if (request.GithubUrl != null) payload["github_url"] = request.GithubUrl.Trim();
        if (request.PortfolioUrl != null) payload["portfolio_url"] = request.PortfolioUrl.Trim();
        if (request.CountryCode != null) payload["country_code"] = request.CountryCode.Trim();
        if (request.StateProvince != null) payload["state_province"] = request.StateProvince.Trim();
        if (request.City != null) payload["city"] = request.City.Trim();
        if (request.YearsOfExperience.HasValue) payload["years_of_experience"] = request.YearsOfExperience.Value;
        if (request.IsProfileComplete.HasValue) payload["is_profile_complete"] = request.IsProfileComplete.Value;

        payload["updated_at"] = DateTime.UtcNow;

        var updated = await _supabase.UpdateAsync<UserProfile>($"users?id=eq.{userId}", payload);
        if (updated == null) throw new KeyNotFoundException($"User profile {userId} not found.");

        return MapToDto(updated);
    }

    private static UserProfileDto MapToDto(UserProfile u)
    {
        return new UserProfileDto
        {
            Id = u.Id,
            Email = u.Email,
            FirstName = u.FirstName,
            LastName = u.LastName,
            DisplayName = u.DisplayName,
            Username = u.Username,
            AvatarPath = u.AvatarPath,
            CoverPath = u.CoverPath,
            Phone = u.Phone,
            Headline = u.Headline,
            CurrentJobTitle = u.CurrentJobTitle,
            CurrentCompany = u.CurrentCompany,
            Bio = u.Bio,
            WebsiteUrl = u.WebsiteUrl,
            LinkedinUrl = u.LinkedinUrl,
            GithubUrl = u.GithubUrl,
            PortfolioUrl = u.PortfolioUrl,
            CountryCode = u.CountryCode,
            StateProvince = u.StateProvince,
            City = u.City,
            YearsOfExperience = u.YearsOfExperience,
            Role = u.Role,
            Status = u.Status,
            IsProfileComplete = u.IsProfileComplete,
            CreatedAt = u.CreatedAt,
            UpdatedAt = u.UpdatedAt
        };
    }
}

