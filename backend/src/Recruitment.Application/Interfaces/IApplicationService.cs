using Recruitment.Application.DTOs;

namespace Recruitment.Application.Interfaces;

public interface IApplicationService
{
    Task<bool> CheckUserAppliedAsync(Guid jobId, Guid userId);
    Task<JobApplicationDto?> FetchUserApplicationAsync(Guid jobId, Guid userId);
    Task<JobApplicationDto> ApplyForJobAsync(CreateApplicationRequest request);
    Task<List<JobApplicationDto>> FetchApplicationsForJobAsync(Guid jobId);
    Task<List<JobApplicationDto>> FetchMyApplicationsAsync(Guid userId);
    Task WithdrawApplicationAsync(Guid applicationId, Guid userId);
    Task UpdateApplicationStatusAsync(Guid applicationId, string status);
    Task<Guid> GetApplicationIdBySlugAsync(string slug);
}

