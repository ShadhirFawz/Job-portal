using System.Text.Json.Serialization;

namespace Recruitment.Domain.Entities;

public class Job
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
    public bool IsSelectionProcessPublic { get; set; } = true;

    [JsonPropertyName("job_type")]
    public string JobType { get; set; } = "full_time";

    [JsonPropertyName("work_mode")]
    public string WorkMode { get; set; } = "onsite";

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
    public string? SalaryCurrency { get; set; } = "INR";

    [JsonPropertyName("salary_period")]
    public string? SalaryPeriod { get; set; } = "year";

    [JsonPropertyName("show_salary")]
    public bool ShowSalary { get; set; }

    [JsonPropertyName("application_deadline")]
    public DateTime? ApplicationDeadline { get; set; }

    [JsonPropertyName("max_applications")]
    public int? MaxApplications { get; set; }

    [JsonPropertyName("accept_applications")]
    public bool AcceptApplications { get; set; } = true;

    [JsonPropertyName("status")]
    public string Status { get; set; } = "draft";

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
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    // Joined relations
    [JsonPropertyName("organization")]
    public Organization? Organization { get; set; }

    [JsonPropertyName("selection_processes")]
    public List<SelectionProcess>? SelectionProcesses { get; set; }
}

