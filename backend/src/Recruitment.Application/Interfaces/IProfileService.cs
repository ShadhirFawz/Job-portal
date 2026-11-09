using Recruitment.Application.DTOs;

namespace Recruitment.Application.Interfaces;

public interface IProfileService
{
    Task<UserProfileDto?> GetUserProfileAsync(Guid userId);
    Task<UserProfileDto?> GetUserProfileByUsernameAsync(string username);
    Task<UsernameCheckResultDto> CheckUsernameAvailabilityAsync(string username, Guid? currentUserId = null);
    Task<UserProfileDto> UpdateUserProfileAsync(Guid userId, UpdateProfileRequest request);
}

