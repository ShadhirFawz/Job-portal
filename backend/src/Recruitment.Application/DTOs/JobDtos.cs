using System.Text.Json.Serialization;

namespace Recruitment.Application.DTOs;

public class JobFilterRequest
{
    [JsonPropertyName("q")]
    public string? Search { get; set; }

    [JsonPropertyName("type")]
    public string? JobType { get; set; }

    [JsonPropertyName("mode")]
    public string? WorkMode { get; set; }

    [JsonPropertyName("level")]
    public string? ExperienceLevel { get; set; }

    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [JsonPropertyName("department")]
    public string? Department { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; } = 1;

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; } = 12;
}

public class PagedResult<T>
{
    [JsonPropertyName("items")]
    public List<T> Items { get; set; } = [];

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; } = 1;

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; } = 12;

    [JsonPropertyName("totalPages")]
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}

public class SelectionProcessDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("job_id")]
    public Guid JobId { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("process_type")]
    public string ProcessType { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("order_index")]
    public int OrderIndex { get; set; }

    [JsonPropertyName("is_mandatory")]
    public bool IsMandatory { get; set; }

    [JsonPropertyName("time_limit_minutes")]
    public int? TimeLimitMinutes { get; set; }

    [JsonPropertyName("passing_score")]
    public int? PassingScore { get; set; }

    [JsonPropertyName("config")]
    public Dictionary<string, object>? Config { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

public class JobDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("organization_id")]
    public Guid OrganizationId { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string? Slug { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("short_description")]
    public string? ShortDescription { get; set; }

    [JsonPropertyName("responsibilities")]
    public string? Responsibilities { get; set; }

    [JsonPropertyName("requirements")]
    public string? Requirements { get; set; }

    [JsonPropertyName("benefits")]
    public string? Benefits { get; set; }

    [JsonPropertyName("nice_to_have")]
    public string? NiceToHave { get; set; }

    [JsonPropertyName("what_we_offer")]
    public string? WhatWeOffer { get; set; }

    [JsonPropertyName("tech_stacks")]
    public List<string>? TechStacks { get; set; }

    [JsonPropertyName("has_custom_hiring_process")]
    public bool HasCustomHiringProcess { get; set; }

    [JsonPropertyName("how_to_apply")]
    public string? HowToApply { get; set; }

    [JsonPropertyName("is_selection_process_public")]
    public bool IsSelectionProcessPublic { get; set; }

    [JsonPropertyName("job_type")]
    public string JobType { get; set; } = string.Empty;

    [JsonPropertyName("work_mode")]
    public string WorkMode { get; set; } = string.Empty;

    [JsonPropertyName("experience_level")]
    public string? ExperienceLevel { get; set; }

    [JsonPropertyName("department")]
    public string? Department { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("country_code")]
    public string? CountryCode { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("state_province")]
    public string? StateProvince { get; set; }

    [JsonPropertyName("is_remote_friendly")]
    public bool IsRemoteFriendly { get; set; }

    [JsonPropertyName("salary_min")]
    public int? SalaryMin { get; set; }

    [JsonPropertyName("salary_max")]
    public int? SalaryMax { get; set; }

    [JsonPropertyName("salary_currency")]
    public string? SalaryCurrency { get; set; }

    [JsonPropertyName("salary_period")]
    public string? SalaryPeriod { get; set; }

    [JsonPropertyName("show_salary")]
    public bool ShowSalary { get; set; }

    [JsonPropertyName("application_deadline")]
    public DateTime? ApplicationDeadline { get; set; }

    [JsonPropertyName("max_applications")]
    public int? MaxApplications { get; set; }

    [JsonPropertyName("accept_applications")]
    public bool AcceptApplications { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("published_at")]
    public DateTime? PublishedAt { get; set; }

    [JsonPropertyName("closed_at")]
    public DateTime? ClosedAt { get; set; }

    [JsonPropertyName("views_count")]
    public int ViewsCount { get; set; }

    [JsonPropertyName("applications_count")]
    public int ApplicationsCount { get; set; }

    [JsonPropertyName("created_by")]
    public Guid CreatedBy { get; set; }

    [JsonPropertyName("updated_by")]
    public Guid? UpdatedBy { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("organization")]
    public OrganizationDto? Organization { get; set; }

    [JsonPropertyName("selection_processes")]
    public List<SelectionProcessDto>? SelectionProcesses { get; set; }
}

