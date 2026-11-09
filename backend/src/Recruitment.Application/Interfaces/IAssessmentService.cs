using Recruitment.Application.DTOs;

namespace Recruitment.Application.Interfaces;

public interface IAssessmentService
{
    Task<QuizAttemptDto> StartAssessmentAsync(Guid userId, StartAssessmentRequest request);
    Task<QuizAttemptDto?> GetAttemptByIdAsync(Guid attemptId, Guid userId);
    Task<QuizAttemptDto?> GetUserStageAttemptAsync(Guid selectionProcessId, Guid userId);
    Task<QuizAttemptDto> SubmitAssessmentAsync(Guid userId, SubmitAssessmentRequest request);
    Task<List<QuizAttemptDto>> GetJobAttemptsAsync(Guid jobId, Guid requestingUserId);
    Task<QuizAttemptDto?> GetApplicationAttemptAsync(Guid applicationId, Guid requestingUserId);
}

