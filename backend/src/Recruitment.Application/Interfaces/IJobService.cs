using Recruitment.Application.DTOs;

namespace Recruitment.Application.Interfaces;

public interface IJobService
{
    Task<PagedResult<JobDto>> GetPublishedJobsAsync(JobFilterRequest filters);
    Task<JobDto?> GetJobByIdAsync(Guid id, bool publicOnly = false);
    Task<List<JobDto>> GetOrganizationJobsAsync(Guid organizationId);
    Task<JobDto> UpdateJobAsync(Guid id, Guid userId, UpdateJobRequest request);
    Task<JobDto> PublishJobAsync(Guid id, Guid userId);
}

