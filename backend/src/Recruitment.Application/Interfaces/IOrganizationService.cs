using Recruitment.Application.DTOs;

namespace Recruitment.Application.Interfaces;

public interface IOrganizationService
{
    Task<OrganizationDto?> GetOrganizationAsync(Guid id);
    Task<List<OrganizationDto>> GetMyOrganizationsAsync(Guid userId);
    Task<bool> CheckIsOrgAdminAsync(Guid orgId, Guid userId, string? userRole = null, Guid? orgCreatedBy = null);
    Task<bool> CheckIsOrgAdminOrStaffAsync(Guid orgId, Guid userId, string? userRole = null, Guid? orgCreatedBy = null);
    Task<OrganizationDto> CreateOrganizationAsync(CreateOrganizationRequest request, Guid userId);
    Task<OrganizationDto> UpdateOrganizationAsync(Guid id, UpdateOrganizationRequest request);
    Task<EmailVerificationTokenResponse> CreateEmailVerificationTokenAsync(Guid orgId, string email, Guid userId, string? origin = null);
    Task<VerifyEmailTokenResponse> VerifyEmailTokenAsync(string token);
}

