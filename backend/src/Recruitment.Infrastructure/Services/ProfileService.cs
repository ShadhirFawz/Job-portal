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

    public async Task<UserProfileDto?> GetUserProfileAsync(Guid userId)
    {
        if (userId == Guid.Empty) return null;

        var profile = await _supabase.GetSingleAsync<UserProfile>($"users?id=eq.{userId}&select=*");
        return profile != null ? MapToDto(profile) : null;
    }

    public async Task<UserProfileDto> UpdateUserProfileAsync(Guid userId, UpdateProfileRequest request)
    {
        var payload = new Dictionary<string, object?>();

        if (request.FirstName != null) payload["first_name"] = request.FirstName.Trim();
        if (request.LastName != null) payload["last_name"] = request.LastName.Trim();
        if (request.DisplayName != null) payload["display_name"] = request.DisplayName.Trim();
        if (request.AvatarPath != null) payload["avatar_path"] = request.AvatarPath;
        if (request.CoverPath != null) payload["cover_path"] = request.CoverPath;
        if (request.Phone != null) payload["phone"] = request.Phone.Trim();
        if (request.Headline != null) payload["headline"] = request.Headline.Trim();
        if (request.CurrentJobTitle != null) payload["current_job_title"] = request.CurrentJobTitle.Trim();
        if (request.CurrentCompany != null) payload["current_company"] = request.CurrentCompany.Trim();
        if (request.Bio != null) payload["bio"] = request.Bio.Trim();
        if (request.CountryCode != null) payload["country_code"] = request.CountryCode.Trim();
        if (request.City != null) payload["city"] = request.City.Trim();
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
            AvatarPath = u.AvatarPath,
            CoverPath = u.CoverPath,
            Phone = u.Phone,
            Headline = u.Headline,
            CurrentJobTitle = u.CurrentJobTitle,
            CurrentCompany = u.CurrentCompany,
            Bio = u.Bio,
            CountryCode = u.CountryCode,
            City = u.City,
            Role = u.Role,
            Status = u.Status,
            IsProfileComplete = u.IsProfileComplete,
            CreatedAt = u.CreatedAt,
            UpdatedAt = u.UpdatedAt
        };
    }
}

