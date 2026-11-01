using Recruitment.Application.DTOs;
using Recruitment.Application.Interfaces;
using Recruitment.Domain.Entities;
using Recruitment.Infrastructure.Supabase;

namespace Recruitment.Infrastructure.Services;

public class ResumeService : IResumeService
{
    private readonly ISupabaseContext _supabase;

    public ResumeService(ISupabaseContext supabase)
    {
        _supabase = supabase;
    }

    public async Task<List<ResumeDto>> FetchUserResumesAsync(Guid userId)
    {
        if (userId == Guid.Empty) return [];

        var list = await _supabase.GetListAsync<Resume>(
            $"resumes?user_id=eq.{userId}&deleted_at=is.null&select=*&order=is_primary.desc,created_at.desc");

        return list.Select(MapToDto).ToList();
    }

    public async Task<ResumeDto?> FetchResumeByIdAsync(Guid resumeId)
    {
        if (resumeId == Guid.Empty) return null;

        var resume = await _supabase.GetSingleAsync<Resume>($"resumes?id=eq.{resumeId}&select=*");
        return resume != null ? MapToDto(resume) : null;
    }

    public async Task<ResumeDto> CreateResumeAsync(CreateResumeRequest request)
    {
        var payload = new Dictionary<string, object?>
        {
            ["user_id"] = request.UserId,
            ["title"] = request.Title.Trim(),
            ["file_path"] = request.FilePath,
            ["file_name"] = request.FileName,
            ["file_size"] = request.FileSize,
            ["file_type"] = request.FileType,
            ["is_primary"] = request.IsPrimary ?? false
        };

        var created = await _supabase.InsertAsync<Resume>("resumes", payload);
        return MapToDto(created);
    }

    public async Task<ResumeDto> UpdateResumeAsync(Guid resumeId, UpdateResumeRequest request)
    {
        var payload = new Dictionary<string, object?>
        {
            ["updated_at"] = DateTime.UtcNow
        };

        if (request.Title != null) payload["title"] = request.Title.Trim();
        if (request.IsPrimary.HasValue) payload["is_primary"] = request.IsPrimary.Value;
        if (request.Summary != null) payload["summary"] = request.Summary;
        if (request.Skills != null) payload["skills"] = request.Skills;

        var updated = await _supabase.UpdateAsync<Resume>($"resumes?id=eq.{resumeId}", payload);
        if (updated == null) throw new KeyNotFoundException($"Resume {resumeId} not found.");

        return MapToDto(updated);
    }

    public async Task SetPrimaryResumeAsync(Guid resumeId)
    {
        await _supabase.UpdateAsync<Resume>(
            $"resumes?id=eq.{resumeId}",
            new { is_primary = true });
    }

    public async Task DeleteResumeAsync(Guid resumeId)
    {
        var resume = await _supabase.GetSingleAsync<Resume>($"resumes?id=eq.{resumeId}&select=id,file_path");
        if (resume == null) return;

        // 1. Soft-delete database record
        await _supabase.UpdateAsync<Resume>(
            $"resumes?id=eq.{resumeId}",
            new
            {
                deleted_at = DateTime.UtcNow,
                is_primary = false
            });

        // 2. Remove file from storage
        if (!string.IsNullOrWhiteSpace(resume.FilePath))
        {
            await _supabase.DeleteStorageObjectAsync("resumes", resume.FilePath);
        }
    }

    public async Task<string?> GetDownloadUrlAsync(string filePath, int expiresIn = 3600)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return null;
        return await _supabase.CreateSignedUrlAsync("resumes", filePath, expiresIn);
    }

    private static ResumeDto MapToDto(Resume r)
    {
        return new ResumeDto
        {
            Id = r.Id,
            UserId = r.UserId,
            Title = r.Title,
            IsPrimary = r.IsPrimary,
            FilePath = r.FilePath,
            FileName = r.FileName,
            FileSize = r.FileSize,
            FileType = r.FileType,
            Summary = r.Summary,
            Skills = r.Skills,
            IsPublic = r.IsPublic,
            LastUsedAt = r.LastUsedAt,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt,
            DeletedAt = r.DeletedAt
        };
    }
}

