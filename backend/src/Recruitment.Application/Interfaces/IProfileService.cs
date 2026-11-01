using Recruitment.Application.DTOs;

namespace Recruitment.Application.Interfaces;

public interface IProfileService
{
    Task<UserProfileDto?> GetUserProfileAsync(Guid userId);
    Task<UserProfileDto> UpdateUserProfileAsync(Guid userId, UpdateProfileRequest request);
}

