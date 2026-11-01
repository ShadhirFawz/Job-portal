using Recruitment.Application.DTOs;

namespace Recruitment.Application.Interfaces;

public interface IJobService
{
    Task<PagedResult<JobDto>> GetPublishedJobsAsync(JobFilterRequest filters);
    Task<JobDto?> GetJobByIdAsync(Guid id, bool publicOnly = false);
    Task<List<JobDto>> GetOrganizationJobsAsync(Guid organizationId);
}

