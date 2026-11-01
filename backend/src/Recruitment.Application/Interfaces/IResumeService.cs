using Recruitment.Application.DTOs;

namespace Recruitment.Application.Interfaces;

public interface IResumeService
{
    Task<List<ResumeDto>> FetchUserResumesAsync(Guid userId);
    Task<ResumeDto?> FetchResumeByIdAsync(Guid resumeId);
    Task<ResumeDto> CreateResumeAsync(CreateResumeRequest request);
    Task<ResumeDto> UpdateResumeAsync(Guid resumeId, UpdateResumeRequest request);
    Task SetPrimaryResumeAsync(Guid resumeId);
    Task DeleteResumeAsync(Guid resumeId);
    Task<string?> GetDownloadUrlAsync(string filePath, int expiresIn = 3600);
}

