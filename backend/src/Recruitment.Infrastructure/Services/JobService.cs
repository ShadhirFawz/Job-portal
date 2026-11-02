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

        var query = "jobs?status=eq.published&deleted_at=is.null";

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
