using Recruitment.Application.DTOs;
using Recruitment.Application.Interfaces;
using Recruitment.Domain.Entities;
using Recruitment.Infrastructure.Supabase;

namespace Recruitment.Infrastructure.Services;

public class OrganizationService : IOrganizationService
{
    private readonly ISupabaseContext _supabase;

    public OrganizationService(ISupabaseContext supabase)
    {
        _supabase = supabase;
    }

    public async Task<OrganizationDto?> GetOrganizationAsync(Guid id)
    {
        var org = await _supabase.GetSingleAsync<Organization>($"organizations?id=eq.{id}&select=*");
        return org != null ? MapToDto(org) : null;
    }

    public async Task<List<OrganizationDto>> GetMyOrganizationsAsync(Guid userId)
    {
        var members = await _supabase.GetListAsync<OrganizationMember>(
            $"organization_members?user_id=eq.{userId}&status=eq.active&select=role,status,organization:organizations(*)");

        return members
            .Where(m => m.Organization != null)
            .Select(m => MapToDto(m.Organization!))
            .ToList();
    }

    public async Task<bool> CheckIsOrgAdminAsync(Guid orgId, Guid userId, string? userRole = null, Guid? orgCreatedBy = null)
    {
        if (orgCreatedBy.HasValue && orgCreatedBy.Value == userId) return true;
        if (string.Equals(userRole, "super_admin", StringComparison.OrdinalIgnoreCase)) return true;

        var member = await _supabase.GetSingleAsync<OrganizationMember>(
            $"organization_members?organization_id=eq.{orgId}&user_id=eq.{userId}&status=eq.active&select=role,status");

        return member?.Role == "org_admin";
    }

    public async Task<bool> CheckIsOrgAdminOrStaffAsync(Guid orgId, Guid userId, string? userRole = null, Guid? orgCreatedBy = null)
    {
        if (orgCreatedBy.HasValue && orgCreatedBy.Value == userId) return true;
        if (string.Equals(userRole, "super_admin", StringComparison.OrdinalIgnoreCase)) return true;

        var member = await _supabase.GetSingleAsync<OrganizationMember>(
            $"organization_members?organization_id=eq.{orgId}&user_id=eq.{userId}&status=eq.active&select=role,status");

        return member?.Role is "org_admin" or "org_staff";
    }

    public async Task<OrganizationDto> CreateOrganizationAsync(CreateOrganizationRequest request, Guid userId)
    {
        var orgPayload = new Dictionary<string, object?>
        {
            ["name"] = request.Name.Trim(),
            ["legal_name"] = string.IsNullOrWhiteSpace(request.LegalName) ? null : request.LegalName.Trim(),
            ["official_email"] = request.OfficialEmail.Trim().ToLowerInvariant(),
            ["tagline"] = string.IsNullOrWhiteSpace(request.Tagline) ? null : request.Tagline.Trim(),
            ["description"] = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            ["website_url"] = string.IsNullOrWhiteSpace(request.WebsiteUrl) ? null : request.WebsiteUrl.Trim(),
            ["phone"] = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            ["industry"] = string.IsNullOrWhiteSpace(request.Industry) ? null : request.Industry.Trim(),
            ["company_size"] = request.CompanySize,
            ["founded_year"] = request.FoundedYear,
            ["company_type"] = string.IsNullOrWhiteSpace(request.CompanyType) ? null : request.CompanyType.Trim(),
            ["has_physical_office"] = request.HasPhysicalOffice ?? false,
            ["country_code"] = string.IsNullOrWhiteSpace(request.CountryCode) ? null : request.CountryCode.Trim(),
            ["state_province"] = string.IsNullOrWhiteSpace(request.StateProvince) ? null : request.StateProvince.Trim(),
            ["city"] = string.IsNullOrWhiteSpace(request.City) ? null : request.City.Trim(),
            ["postal_code"] = string.IsNullOrWhiteSpace(request.PostalCode) ? null : request.PostalCode.Trim(),
            ["address_line1"] = string.IsNullOrWhiteSpace(request.AddressLine1) ? null : request.AddressLine1.Trim(),
            ["address_line2"] = string.IsNullOrWhiteSpace(request.AddressLine2) ? null : request.AddressLine2.Trim(),
            ["latitude"] = request.Latitude,
            ["longitude"] = request.Longitude,
            ["avatar_path"] = request.AvatarPath,
            ["cover_path"] = request.CoverPath,
            ["created_by"] = userId,
            ["status"] = "pending_verification"
        };

        var createdOrg = await _supabase.InsertAsync<Organization>("organizations", orgPayload);

        // Auto-assign creator as active org_admin
        try
        {
            var memberPayload = new Dictionary<string, object?>
            {
                ["organization_id"] = createdOrg.Id,
                ["user_id"] = userId,
                ["role"] = "org_admin",
                ["status"] = "active",
                ["joined_at"] = DateTime.UtcNow,
                ["title"] = "Founder / Administrator"
            };
            await _supabase.InsertAsync<OrganizationMember>("organization_members", memberPayload);
        }
        catch (Exception ex)
        {
            // Log warning but continue, as organization is created
            Console.WriteLine($"Could not auto-add creator to organization_members: {ex.Message}");
        }

        return MapToDto(createdOrg);
    }

    public async Task<OrganizationDto> UpdateOrganizationAsync(Guid id, UpdateOrganizationRequest request)
    {
        var payload = new Dictionary<string, object?>();

        if (request.Name != null) payload["name"] = request.Name.Trim();
        if (request.LegalName != null) payload["legal_name"] = request.LegalName.Trim();
        if (request.Tagline != null) payload["tagline"] = request.Tagline.Trim();
        if (request.Description != null) payload["description"] = request.Description.Trim();
        if (request.WebsiteUrl != null) payload["website_url"] = request.WebsiteUrl.Trim();
        if (request.Phone != null) payload["phone"] = request.Phone.Trim();
        if (request.Industry != null) payload["industry"] = request.Industry.Trim();
        if (request.CompanySize != null) payload["company_size"] = request.CompanySize;
        if (request.FoundedYear.HasValue) payload["founded_year"] = request.FoundedYear.Value;
        if (request.CompanyType != null) payload["company_type"] = request.CompanyType.Trim();
        if (request.HasPhysicalOffice.HasValue) payload["has_physical_office"] = request.HasPhysicalOffice.Value;
        if (request.CountryCode != null) payload["country_code"] = request.CountryCode.Trim();
        if (request.StateProvince != null) payload["state_province"] = request.StateProvince.Trim();
        if (request.City != null) payload["city"] = request.City.Trim();
        if (request.PostalCode != null) payload["postal_code"] = request.PostalCode.Trim();
        if (request.AddressLine1 != null) payload["address_line1"] = request.AddressLine1.Trim();
        if (request.AddressLine2 != null) payload["address_line2"] = request.AddressLine2.Trim();
        if (request.Latitude.HasValue) payload["latitude"] = request.Latitude.Value;
        if (request.Longitude.HasValue) payload["longitude"] = request.Longitude.Value;
        if (request.AvatarPath != null) payload["avatar_path"] = request.AvatarPath;
        if (request.CoverPath != null) payload["cover_path"] = request.CoverPath;

        var updated = await _supabase.UpdateAsync<Organization>($"organizations?id=eq.{id}", payload);
        if (updated == null) throw new KeyNotFoundException($"Organization {id} not found.");

        return MapToDto(updated);
    }

    public async Task<EmailVerificationTokenResponse> CreateEmailVerificationTokenAsync(Guid orgId, string email, Guid userId, string? origin = null)
    {
        var token = $"{Guid.NewGuid()}-{Guid.NewGuid()}";
        var expiresAt = DateTime.UtcNow.AddHours(24);

        // Delete unverified previous tokens
        await _supabase.DeleteAsync($"organization_email_verifications?organization_id=eq.{orgId}&verified_at=is.null");

        var payload = new Dictionary<string, object?>
        {
            ["organization_id"] = orgId,
            ["email"] = email.Trim().ToLowerInvariant(),
            ["token"] = token,
            ["expires_at"] = expiresAt,
            ["created_by"] = userId
        };

        await _supabase.InsertAsync<OrganizationEmailVerification>("organization_email_verifications", payload);

        var baseOrigin = !string.IsNullOrWhiteSpace(origin) ? origin.TrimEnd('/') : "http://localhost:5173";
        var verifyUrl = $"{baseOrigin}/organizations/verify-email?token={token}";

        return new EmailVerificationTokenResponse
        {
            Token = token,
            VerifyUrl = verifyUrl
        };
    }

    public async Task<VerifyEmailTokenResponse> VerifyEmailTokenAsync(string token)
    {
        var record = await _supabase.GetSingleAsync<OrganizationEmailVerification>(
            $"organization_email_verifications?token=eq.{token}&verified_at=is.null&select=*,organization:organizations(id,name,official_email)");

        if (record == null)
        {
            throw new InvalidOperationException("Invalid or already used verification link.");
        }

        if (record.ExpiresAt < DateTime.UtcNow)
        {
            throw new InvalidOperationException("This verification link has expired. Please request a new one.");
        }

        // Mark token verified
        await _supabase.UpdateAsync<OrganizationEmailVerification>(
            $"organization_email_verifications?id=eq.{record.Id}",
            new { verified_at = DateTime.UtcNow });

        // Update organization official_email_verified
        await _supabase.UpdateAsync<Organization>(
            $"organizations?id=eq.{record.OrganizationId}",
            new
            {
                official_email_verified = true,
                status = "active",
                verified_at = DateTime.UtcNow
            });

        return new VerifyEmailTokenResponse
        {
            OrganizationId = record.OrganizationId,
            OrganizationName = record.Organization?.Name ?? string.Empty,
            OfficialEmail = record.Organization?.OfficialEmail ?? string.Empty
        };
    }

    private static OrganizationDto MapToDto(Organization org)
    {
        return new OrganizationDto
        {
            Id = org.Id,
            Name = org.Name,
            LegalName = org.LegalName,
            OfficialEmail = org.OfficialEmail,
            OfficialEmailVerified = org.OfficialEmailVerified,
            Tagline = org.Tagline,
            Description = org.Description,
            WebsiteUrl = org.WebsiteUrl,
            Phone = org.Phone,
            Industry = org.Industry,
            CompanySize = org.CompanySize,
            FoundedYear = org.FoundedYear,
            CompanyType = org.CompanyType,
            HasPhysicalOffice = org.HasPhysicalOffice,
            CountryCode = org.CountryCode,
            StateProvince = org.StateProvince,
            City = org.City,
            PostalCode = org.PostalCode,
            AddressLine1 = org.AddressLine1,
            AddressLine2 = org.AddressLine2,
            Latitude = org.Latitude,
            Longitude = org.Longitude,
            AvatarPath = org.AvatarPath,
            CoverPath = org.CoverPath,
            Status = org.Status,
            VerifiedAt = org.VerifiedAt,
            CreatedBy = org.CreatedBy,
            CreatedAt = org.CreatedAt,
            UpdatedAt = org.UpdatedAt
        };
    }
}

