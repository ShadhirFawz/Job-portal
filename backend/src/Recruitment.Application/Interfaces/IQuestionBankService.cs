using Recruitment.Application.DTOs;

namespace Recruitment.Application.Interfaces;

public interface IQuestionBankService
{
    Task<List<QuestionBankDto>> GetOrganizationBanksAsync(Guid organizationId);
    Task<QuestionBankDto?> GetBankByIdAsync(Guid bankId);
    Task<QuestionBankDto> CreateBankAsync(Guid organizationId, Guid userId, CreateQuestionBankRequest request);
    Task<QuestionBankDto> UpdateBankAsync(Guid bankId, Guid userId, UpdateQuestionBankRequest request);
    Task DeleteBankAsync(Guid bankId, Guid userId);

    Task<QuestionDto> AddQuestionAsync(Guid bankId, Guid userId, CreateQuestionRequest request);
    Task<QuestionDto> UpdateQuestionAsync(Guid questionId, Guid userId, UpdateQuestionRequest request);
    Task DeleteQuestionAsync(Guid questionId, Guid userId);
}

