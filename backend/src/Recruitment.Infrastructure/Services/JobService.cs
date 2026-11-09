using System.Text.Encodings.Web;
using Recruitment.Application.DTOs;
using Recruitment.Application.Interfaces;
using Recruitment.Domain.Entities;
using Recruitment.Infrastructure.Supabase;

namespace Recruitment.Infrastructure.Services;

public class JobService : IJobService
{
    private readonly ISupabaseContext _supabase;

    public JobService(ISupabaseContext supabase)
    {
        _supabase = supabase;
    }

    public async Task<PagedResult<JobDto>> GetPublishedJobsAsync(JobFilterRequest filters)
    {
        var page = Math.Max(1, filters.Page);
        var pageSize = filters.PageSize > 0 && filters.PageSize <= 50 ? filters.PageSize : 12;

        // Only published jobs whose deadline hasn't passed (or have no deadline set)
        var query = "jobs?status=eq.published&deleted_at=is.null&or=(application_deadline.is.null,application_deadline.gt.now())";

        var search = !string.IsNullOrWhiteSpace(filters.Search) ? filters.Search : filters.Q;
        var jobType = !string.IsNullOrWhiteSpace(filters.JobType) ? filters.JobType : filters.Type;
        var workMode = !string.IsNullOrWhiteSpace(filters.WorkMode) ? filters.WorkMode : filters.Mode;
        var expLevel = !string.IsNullOrWhiteSpace(filters.ExperienceLevel) ? filters.ExperienceLevel : filters.Level;
        var location = filters.Location;
        var department = filters.Department;

        // Search query
        if (!string.IsNullOrWhiteSpace(search))
        {
            var rawSearch = search.Trim().Replace("(", "").Replace(")", "").Replace(",", " ");
            var escaped = UrlEncoder.Default.Encode(rawSearch);
            query += $"&or=(title.ilike.*{escaped}*,short_description.ilike.*{escaped}*,description.ilike.*{escaped}*)";
        }

        // Job type
        if (!string.IsNullOrWhiteSpace(jobType))
        {
            query += $"&job_type=eq.{jobType.Trim()}";
        }

        // Work mode
        if (!string.IsNullOrWhiteSpace(workMode))
        {
            query += $"&work_mode=eq.{workMode.Trim()}";
        }

        // Experience level
        if (!string.IsNullOrWhiteSpace(expLevel))
        {
            query += $"&experience_level=eq.{expLevel.Trim()}";
        }

        // Department
        if (!string.IsNullOrWhiteSpace(department))
        {
            var dept = UrlEncoder.Default.Encode(department.Trim());
            query += $"&department=ilike.*{dept}*";
        }

        // Location
        if (!string.IsNullOrWhiteSpace(location))
        {
            var rawLoc = location.Trim().Replace("(", "").Replace(")", "").Replace(",", " ");
            var escapedLoc = UrlEncoder.Default.Encode(rawLoc);
            query += $"&or=(city.ilike.*{escapedLoc}*,country_code.ilike.*{escapedLoc}*,state_province.ilike.*{escapedLoc}*)";
        }

        query += "&select=*,organization:organizations(id,name,slug,avatar_path,industry,city,country_code,website_url)&order=published_at.desc";

        var (items, totalCount) = await _supabase.GetPagedListAsync<Job>(query, page, pageSize);

        return new PagedResult<JobDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<JobDto?> GetJobByIdAsync(Guid id, bool publicOnly = false)
    {
        var query = $"jobs?id=eq.{id}&deleted_at=is.null";
        if (publicOnly)
        {
            query += "&status=eq.published";
        }

        query += "&select=*,organization:organizations(id,name,slug,avatar_path,industry,city,country_code,website_url),selection_processes:job_selection_processes(*)";

        var job = await _supabase.GetSingleAsync<Job>(query);
        return job != null ? MapToDto(job) : null;
    }

    public async Task<List<JobDto>> GetOrganizationJobsAsync(Guid organizationId)
    {
        var query = $"jobs?organization_id=eq.{organizationId}&deleted_at=is.null&order=created_at.desc" +
                     "&select=*,selection_processes:job_selection_processes(*)";

        var list = await _supabase.GetListAsync<Job>(query);
        return list.Select(MapToDto).ToList();
    }

    public async Task<JobDto> UpdateJobAsync(Guid id, Guid userId, UpdateJobRequest request)
    {
        var existing = await _supabase.GetSingleAsync<Job>($"jobs?id=eq.{id}&deleted_at=is.null");
        if (existing == null) throw new KeyNotFoundException("Job posting not found.");

        if (existing.CreatedBy != userId)
        {
            throw new UnauthorizedAccessException("Only the owner/creator of this job post is authorized to edit it.");
        }

        var payload = new Dictionary<string, object?>
        {
            ["title"] = request.Title?.Trim(),
            ["short_description"] = request.ShortDescription?.Trim(),
            ["description"] = request.Description?.Trim(),
            ["responsibilities"] = request.Responsibilities?.Trim(),
            ["requirements"] = request.Requirements?.Trim(),
            ["benefits"] = request.Benefits?.Trim(),
            ["nice_to_have"] = request.NiceToHave?.Trim(),
            ["what_we_offer"] = request.WhatWeOffer?.Trim(),
            ["tech_stacks"] = request.TechStacks ?? [],
            ["has_custom_hiring_process"] = request.HasCustomHiringProcess,
            ["how_to_apply"] = request.HasCustomHiringProcess ? request.HowToApply?.Trim() : null,
            ["is_selection_process_public"] = request.IsSelectionProcessPublic,
            ["job_type"] = request.JobType,
            ["work_mode"] = request.WorkMode,
            ["experience_level"] = request.ExperienceLevel,
            ["department"] = request.Department?.Trim(),
            ["category"] = request.Category?.Trim(),
            ["country_code"] = request.CountryCode?.Trim(),
            ["city"] = request.City?.Trim(),
            ["salary_min"] = request.SalaryMin,
            ["salary_max"] = request.SalaryMax,
            ["salary_currency"] = request.SalaryCurrency ?? "INR",
            ["show_salary"] = request.ShowSalary,
            ["application_deadline"] = request.ApplicationDeadline,
            ["accept_applications"] = request.AcceptApplications,
            ["updated_by"] = userId,
            ["updated_at"] = DateTime.UtcNow
        };

        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            var slug = System.Text.RegularExpressions.Regex.Replace(request.Title.ToLowerInvariant().Trim(), @"[^a-z0-9]+", "-").Trim('-');
            payload["slug"] = slug;
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            payload["status"] = request.Status;
            if (request.Status == "published" && existing.PublishedAt == null)
            {
                payload["published_at"] = DateTime.UtcNow;
            }
        }

        await _supabase.UpdateAsync<Job>($"jobs?id=eq.{id}", payload);

        // Update selection processes if configured
        if (!request.HasCustomHiringProcess && request.SelectionProcesses != null)
        {
            // Remove existing selection processes
            await _supabase.DeleteAsync($"job_selection_processes?job_id=eq.{id}");

            // Insert updated selection processes
            if (request.SelectionProcesses.Count > 0)
            {
                var idx = 1;
                foreach (var p in request.SelectionProcesses)
                {
                    var procPayload = new
                    {
                        job_id = id,
                        title = string.IsNullOrWhiteSpace(p.Title) ? $"Stage {idx}" : p.Title.Trim(),
                        process_type = p.ProcessType,
                        description = p.Description?.Trim(),
                        order_index = idx++,
                        is_mandatory = p.IsMandatory,
                        time_limit_minutes = p.TimeLimitMinutes,
                        question_bank_id = p.QuestionBankId,
                        config = p.QuestionBankId.HasValue
                            ? new Dictionary<string, object> { ["question_bank_id"] = p.QuestionBankId.Value }
                            : new Dictionary<string, object>()
                    };
                    await _supabase.InsertAsync<SelectionProcess>("job_selection_processes", procPayload);
                }
            }
        }

        return (await GetJobByIdAsync(id))!;
    }

    public async Task<JobDto> PublishJobAsync(Guid id, Guid userId)
    {
        var existing = await _supabase.GetSingleAsync<Job>($"jobs?id=eq.{id}&deleted_at=is.null");
        if (existing == null) throw new KeyNotFoundException("Job posting not found.");

        if (existing.CreatedBy != userId)
        {
            throw new UnauthorizedAccessException("Only the owner/creator of this job post is authorized to publish it.");
        }

        var payload = new Dictionary<string, object?>
        {
            ["status"] = "published",
            ["published_at"] = DateTime.UtcNow,
            ["updated_by"] = userId,
            ["updated_at"] = DateTime.UtcNow
        };

        await _supabase.UpdateAsync<Job>($"jobs?id=eq.{id}", payload);
        return (await GetJobByIdAsync(id))!;
    }

    private static JobDto MapToDto(Job j)
    {
        return new JobDto
        {
            Id = j.Id,
            OrganizationId = j.OrganizationId,
            Title = j.Title,
            Slug = j.Slug,
            Description = j.Description,
            ShortDescription = j.ShortDescription,
            Responsibilities = j.Responsibilities,
            Requirements = j.Requirements,
            Benefits = j.Benefits,
            NiceToHave = j.NiceToHave,
            WhatWeOffer = j.WhatWeOffer,
            TechStacks = j.TechStacks,
            HasCustomHiringProcess = j.HasCustomHiringProcess,
            HowToApply = j.HowToApply,
            IsSelectionProcessPublic = j.IsSelectionProcessPublic,
            JobType = j.JobType,
            WorkMode = j.WorkMode,
            ExperienceLevel = j.ExperienceLevel,
            Department = j.Department,
            Category = j.Category,
            CountryCode = j.CountryCode,
            City = j.City,
            StateProvince = j.StateProvince,
            IsRemoteFriendly = j.IsRemoteFriendly,
            SalaryMin = j.SalaryMin,
            SalaryMax = j.SalaryMax,
            SalaryCurrency = j.SalaryCurrency,
            SalaryPeriod = j.SalaryPeriod,
            ShowSalary = j.ShowSalary,
            ApplicationDeadline = j.ApplicationDeadline,
            MaxApplications = j.MaxApplications,
            AcceptApplications = j.AcceptApplications,
            Status = j.Status,
            PublishedAt = j.PublishedAt,
            ClosedAt = j.ClosedAt,
            ViewsCount = j.ViewsCount,
            ApplicationsCount = j.ApplicationsCount,
            CreatedBy = j.CreatedBy,
            UpdatedBy = j.UpdatedBy,
            CreatedAt = j.CreatedAt,
            UpdatedAt = j.UpdatedAt,
            Organization = j.Organization != null ? MapOrgToDto(j.Organization) : null,
            SelectionProcesses = j.SelectionProcesses?.Select(sp => new SelectionProcessDto
            {
                Id = sp.Id,
                JobId = sp.JobId,
                Title = sp.Title,
                ProcessType = sp.ProcessType,
                Description = sp.Description,
                OrderIndex = sp.OrderIndex,
                IsMandatory = sp.IsMandatory,
                TimeLimitMinutes = sp.TimeLimitMinutes,
                PassingScore = sp.PassingScore,
                QuestionBankId = sp.QuestionBankId,
                Config = sp.Config,
                CreatedAt = sp.CreatedAt,
                UpdatedAt = sp.UpdatedAt
            }).ToList()
        };
    }

    private static OrganizationDto MapOrgToDto(Organization o)
    {
        return new OrganizationDto
        {
            Id = o.Id,
            Name = o.Name,
            LegalName = o.LegalName,
            OfficialEmail = o.OfficialEmail,
            OfficialEmailVerified = o.OfficialEmailVerified,
            Tagline = o.Tagline,
            Description = o.Description,
            WebsiteUrl = o.WebsiteUrl,
            Phone = o.Phone,
            Industry = o.Industry,
            CompanySize = o.CompanySize,
            FoundedYear = o.FoundedYear,
            CompanyType = o.CompanyType,
            HasPhysicalOffice = o.HasPhysicalOffice,
            CountryCode = o.CountryCode,
            StateProvince = o.StateProvince,
            City = o.City,
            PostalCode = o.PostalCode,
            AddressLine1 = o.AddressLine1,
            AddressLine2 = o.AddressLine2,
            Latitude = o.Latitude,
            Longitude = o.Longitude,
            AvatarPath = o.AvatarPath,
            CoverPath = o.CoverPath,
            Status = o.Status,
            VerifiedAt = o.VerifiedAt,
            CreatedBy = o.CreatedBy,
            CreatedAt = o.CreatedAt,
            UpdatedAt = o.UpdatedAt
        };
    }
}
