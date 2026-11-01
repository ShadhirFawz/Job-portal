using Recruitment.Application.DTOs;
using Recruitment.Application.Interfaces;
using Recruitment.Domain.Entities;
using Recruitment.Infrastructure.Supabase;

namespace Recruitment.Infrastructure.Services;

public class ApplicationService : IApplicationService
{
    private readonly ISupabaseContext _supabase;

    public ApplicationService(ISupabaseContext supabase)
    {
        _supabase = supabase;
    }

    public async Task<bool> CheckUserAppliedAsync(Guid jobId, Guid userId)
    {
        if (jobId == Guid.Empty || userId == Guid.Empty) return false;

        var app = await _supabase.GetSingleAsync<JobApplication>(
            $"job_applications?job_id=eq.{jobId}&user_id=eq.{userId}&select=id");

        return app != null;
    }

    public async Task<JobApplicationDto?> FetchUserApplicationAsync(Guid jobId, Guid userId)
    {
        if (jobId == Guid.Empty || userId == Guid.Empty) return null;

        var app = await _supabase.GetSingleAsync<JobApplication>(
            $"job_applications?job_id=eq.{jobId}&user_id=eq.{userId}&select=*");

        return app != null ? MapToDto(app) : null;
    }

    public async Task<JobApplicationDto> ApplyForJobAsync(CreateApplicationRequest request)
    {
        var payload = new Dictionary<string, object?>
        {
            ["job_id"] = request.JobId,
            ["user_id"] = request.UserId,
            ["cover_letter"] = string.IsNullOrWhiteSpace(request.CoverLetter) ? null : request.CoverLetter.Trim(),
            ["resume_id"] = request.ResumeId,
            ["custom_answers"] = request.CustomAnswers ?? new Dictionary<string, object>(),
            ["status"] = string.IsNullOrWhiteSpace(request.Status) ? "applied" : request.Status
        };

        JobApplication createdApp;
        try
        {
            createdApp = await _supabase.InsertAsync<JobApplication>("job_applications", payload);
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("23505") || ex.Message.Contains("unique"))
        {
            throw new InvalidOperationException("You have already applied to this job.");
        }

        // Increment applications count
        try
        {
            await _supabase.RpcVoidAsync("increment_job_applications_count", new { job_id = request.JobId });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"increment_job_applications_count RPC failed, falling back to manual increment: {ex.Message}");
            try
            {
                var job = await _supabase.GetSingleAsync<Dictionary<string, object>>($"jobs?id=eq.{request.JobId}&select=applications_count");
                if (job != null && job.TryGetValue("applications_count", out var countObj) && countObj is System.Text.Json.JsonElement elem && elem.TryGetInt32(out var count))
                {
                    await _supabase.UpdateAsync<Dictionary<string, object>>($"jobs?id=eq.{request.JobId}", new { applications_count = count + 1 });
                }
            }
            catch (Exception fallbackEx)
            {
                Console.WriteLine($"Manual increment fallback also failed: {fallbackEx.Message}");
            }
        }

        return MapToDto(createdApp);
    }

    public async Task<List<JobApplicationDto>> FetchApplicationsForJobAsync(Guid jobId)
    {
        if (jobId == Guid.Empty) return [];

        try
        {
            var query = $"job_applications?job_id=eq.{jobId}" +
                        $"&select=id,job_id,user_id,status,cover_letter,resume_id,custom_answers,current_process_id,score,applied_at,updated_at,withdrawn_at," +
                        $"user:users!user_id(id,email,first_name,last_name,display_name,avatar_path,headline,current_job_title,phone,city,country_code)," +
                        $"resume:resumes!resume_id(id,title,file_name,file_path,file_size,file_type)" +
                        $"&order=applied_at.desc";

            var list = await _supabase.GetListAsync<JobApplication>(query);
            if (list.Count > 0)
            {
                return list.Select(MapToDto).ToList();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Direct FK application join failed in FetchApplicationsForJobAsync: {ex.Message}");
        }

        // Fallback: fetch raw applications and map user and resume separately
        var rawApps = await _supabase.GetListAsync<JobApplication>(
            $"job_applications?job_id=eq.{jobId}&select=*&order=applied_at.desc");

        if (rawApps.Count == 0) return [];

        var userIds = rawApps.Select(a => a.UserId).Distinct().ToList();
        var resumeIds = rawApps.Where(a => a.ResumeId.HasValue).Select(a => a.ResumeId!.Value).Distinct().ToList();

        var usersMap = new Dictionary<Guid, UserProfile>();
        if (userIds.Count > 0)
        {
            var users = await _supabase.GetListAsync<UserProfile>($"users?id=in.({string.Join(",", userIds)})&select=*");
            usersMap = users.ToDictionary(u => u.Id, u => u);
        }

        var resumesMap = new Dictionary<Guid, Resume>();
        if (resumeIds.Count > 0)
        {
            var resumes = await _supabase.GetListAsync<Resume>($"resumes?id=in.({string.Join(",", resumeIds)})&select=*");
            resumesMap = resumes.ToDictionary(r => r.Id, r => r);
        }

        return rawApps.Select(a =>
        {
            var dto = MapToDto(a);
            if (usersMap.TryGetValue(a.UserId, out var u))
            {
                dto.User = new JobApplicantUserDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    DisplayName = u.DisplayName,
                    AvatarPath = u.AvatarPath,
                    Headline = u.Headline,
                    CurrentJobTitle = u.CurrentJobTitle,
                    Phone = u.Phone,
                    City = u.City,
                    CountryCode = u.CountryCode
                };
            }

            if (a.ResumeId.HasValue && resumesMap.TryGetValue(a.ResumeId.Value, out var r))
            {
                dto.Resume = new JobApplicationResumeDto
                {
                    Id = r.Id,
                    Title = r.Title,
                    FileName = r.FileName,
                    FilePath = r.FilePath,
                    FileSize = r.FileSize,
                    FileType = r.FileType
                };
            }

            return dto;
        }).ToList();
    }

    public async Task UpdateApplicationStatusAsync(Guid applicationId, string status)
    {
        await _supabase.UpdateAsync<JobApplication>(
            $"job_applications?id=eq.{applicationId}",
            new
            {
                status,
                updated_at = DateTime.UtcNow
            });
    }

    private static JobApplicationDto MapToDto(JobApplication a)
    {
        return new JobApplicationDto
        {
            Id = a.Id,
            JobId = a.JobId,
            UserId = a.UserId,
            Status = a.Status,
            CoverLetter = a.CoverLetter,
            ResumeId = a.ResumeId,
            CustomAnswers = a.CustomAnswers,
            CurrentProcessId = a.CurrentProcessId,
            Score = a.Score,
            AppliedAt = a.AppliedAt,
            UpdatedAt = a.UpdatedAt,
            WithdrawnAt = a.WithdrawnAt,
            User = a.User != null ? new JobApplicantUserDto
            {
                Id = a.User.Id,
                Email = a.User.Email,
                FirstName = a.User.FirstName,
                LastName = a.User.LastName,
                DisplayName = a.User.DisplayName,
                AvatarPath = a.User.AvatarPath,
                Headline = a.User.Headline,
                CurrentJobTitle = a.User.CurrentJobTitle,
                Phone = a.User.Phone,
                City = a.User.City,
                CountryCode = a.User.CountryCode
            } : null,
            Resume = a.Resume != null ? new JobApplicationResumeDto
            {
                Id = a.Resume.Id,
                Title = a.Resume.Title,
                FileName = a.Resume.FileName,
                FilePath = a.Resume.FilePath,
                FileSize = a.Resume.FileSize,
                FileType = a.Resume.FileType
            } : null
        };
    }
}

