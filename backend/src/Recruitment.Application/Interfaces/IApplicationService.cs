using Recruitment.Application.DTOs;

namespace Recruitment.Application.Interfaces;

public interface IApplicationService
{
    Task<bool> CheckUserAppliedAsync(Guid jobId, Guid userId);
    Task<JobApplicationDto?> FetchUserApplicationAsync(Guid jobId, Guid userId);
    Task<JobApplicationDto> ApplyForJobAsync(CreateApplicationRequest request);
    Task<List<JobApplicationDto>> FetchApplicationsForJobAsync(Guid jobId);
    Task UpdateApplicationStatusAsync(Guid applicationId, string status);
}

