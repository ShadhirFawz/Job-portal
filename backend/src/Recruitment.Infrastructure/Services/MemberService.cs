using Recruitment.Application.DTOs;
using Recruitment.Application.Interfaces;
using Recruitment.Domain.Entities;
using Recruitment.Infrastructure.Supabase;

namespace Recruitment.Infrastructure.Services;

public class MemberService : IMemberService
{
    private readonly ISupabaseContext _supabase;

    public MemberService(ISupabaseContext supabase)
    {
        _supabase = supabase;
    }

    public async Task<List<OrgMemberDto>> FetchMembersAsync(Guid organizationId)
    {
        if (organizationId == Guid.Empty) return [];

        try
        {
            var query = $"organization_members?organization_id=eq.{organizationId}" +
                        $"&select=id,organization_id,user_id,role,status,title,department,joined_at,invited_at,invited_by,revoked_at,revoked_by,rejection_reason," +
                        $"user:users!user_id(id,email,first_name,last_name,display_name,avatar_path)," +
                        $"inviter:users!invited_by(id,email,display_name,first_name,last_name)" +
                        $"&order=created_at.asc";

            var list = await _supabase.GetListAsync<OrganizationMember>(query);
            if (list.Count > 0)
            {
                return list.Select(MapToMemberDto).ToList();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Direct FK join failed in FetchMembersAsync: {ex.Message}");
        }

        // Fallback: fetch raw members then attach users separately
        var rawMembers = await _supabase.GetListAsync<OrganizationMember>(
            $"organization_members?organization_id=eq.{organizationId}&select=*&order=created_at.asc");

        if (rawMembers.Count == 0) return [];

        var userIds = rawMembers.Select(m => m.UserId)
            .Concat(rawMembers.Where(m => m.InvitedBy.HasValue).Select(m => m.InvitedBy!.Value))
            .Distinct()
            .ToList();

        var usersMap = new Dictionary<Guid, UserProfile>();
        if (userIds.Count > 0)
        {
            var idFilter = string.Join(",", userIds);
            var users = await _supabase.GetListAsync<UserProfile>($"users?id=in.({idFilter})&select=*");
            usersMap = users.ToDictionary(u => u.Id, u => u);
        }

        return rawMembers.Select(m =>
        {
            var dto = MapToMemberDto(m);
            if (usersMap.TryGetValue(m.UserId, out var u))
            {
                dto.User = new OrgMemberUserDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    DisplayName = u.DisplayName,
                    AvatarPath = u.AvatarPath
                };
            }
            if (m.InvitedBy.HasValue && usersMap.TryGetValue(m.InvitedBy.Value, out var inv))
            {
                dto.Inviter = new OrgMemberUserDto
                {
                    Id = inv.Id,
                    Email = inv.Email,
                    FirstName = inv.FirstName,
                    LastName = inv.LastName,
                    DisplayName = inv.DisplayName,
                    AvatarPath = inv.AvatarPath
                };
            }
            return dto;
        }).ToList();
    }

    public async Task InviteMemberAsync(InviteMemberRequest request, Guid inviterId)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Please enter a valid email address.");
        }

        // 1. Look up user by email
        UserProfile? targetUser = null;
        try
        {
            var rpcUsers = await _supabase.RpcAsync<List<UserProfile>>("find_user_by_email", new { search_email = email });
            if (rpcUsers != null && rpcUsers.Count > 0)
            {
                targetUser = rpcUsers[0];
            }
        }
        catch
        {
            // Ignore RPC failure and fallback
        }

        if (targetUser == null)
        {
            targetUser = await _supabase.GetSingleAsync<UserProfile>($"users?email=ilike.{email}&select=id,email");
        }

        if (targetUser == null)
        {
            throw new InvalidOperationException("No user found with this email. They must register on the platform first.");
        }

        // 2. Check existing membership
        var existing = await _supabase.GetSingleAsync<OrganizationMember>(
            $"organization_members?organization_id=eq.{request.OrganizationId}&user_id=eq.{targetUser.Id}&select=id,status,role");

        if (existing != null)
        {
            if (existing.Status == "active")
            {
                throw new InvalidOperationException("This user is already an active member of this organization.");
            }
            if (existing.Status == "pending")
            {
                throw new InvalidOperationException("An invitation is already pending for this user.");
            }

            // Re-invite if revoked or rejected
            await _supabase.UpdateAsync<OrganizationMember>(
                $"organization_members?id=eq.{existing.Id}",
                new
                {
                    status = "pending",
                    role = request.Role,
                    title = string.IsNullOrWhiteSpace(request.Title) ? null : request.Title.Trim(),
                    invited_at = DateTime.UtcNow,
                    invited_by = inviterId,
                    revoked_at = (DateTime?)null,
                    revoked_by = (Guid?)null,
                    rejection_reason = (string?)null
                });
            return;
        }

        // 3. Insert new membership
        await _supabase.InsertAsync<OrganizationMember>("organization_members", new
        {
            organization_id = request.OrganizationId,
            user_id = targetUser.Id,
            role = request.Role,
            status = "pending",
            title = string.IsNullOrWhiteSpace(request.Title) ? null : request.Title.Trim(),
            invited_at = DateTime.UtcNow,
            invited_by = inviterId
        });
    }

    public async Task AcceptMemberAsync(Guid memberId)
    {
        await _supabase.UpdateAsync<OrganizationMember>(
            $"organization_members?id=eq.{memberId}",
            new
            {
                status = "active",
                joined_at = DateTime.UtcNow
            });
    }

    public async Task RejectMemberAsync(Guid memberId, string? reason)
    {
        await _supabase.UpdateAsync<OrganizationMember>(
            $"organization_members?id=eq.{memberId}",
            new
            {
                status = "rejected",
                rejection_reason = string.IsNullOrWhiteSpace(reason) ? "Declined by administrator" : reason.Trim()
            });
    }

    public async Task RevokeMemberAsync(Guid memberId, Guid revokedByUserId)
    {
        await _supabase.UpdateAsync<OrganizationMember>(
            $"organization_members?id=eq.{memberId}",
            new
            {
                status = "revoked",
                revoked_at = DateTime.UtcNow,
                revoked_by = revokedByUserId
            });
    }

    public async Task ReinviteMemberAsync(Guid memberId)
    {
        await _supabase.UpdateAsync<OrganizationMember>(
            $"organization_members?id=eq.{memberId}",
            new
            {
                status = "pending",
                invited_at = DateTime.UtcNow,
                revoked_at = (DateTime?)null,
                revoked_by = (Guid?)null,
                rejection_reason = (string?)null
            });
    }

    public async Task DeleteMemberRecordAsync(Guid memberId)
    {
        await _supabase.DeleteAsync($"organization_members?id=eq.{memberId}");
    }

    public async Task UpdateMemberRoleAsync(Guid memberId, string newRole)
    {
        await _supabase.UpdateAsync<OrganizationMember>(
            $"organization_members?id=eq.{memberId}",
            new { role = newRole });
    }

    public async Task UpdateMemberDetailsAsync(Guid memberId, string? title, string? department)
    {
        await _supabase.UpdateAsync<OrganizationMember>(
            $"organization_members?id=eq.{memberId}",
            new
            {
                title = string.IsNullOrWhiteSpace(title) ? null : title.Trim(),
                department = string.IsNullOrWhiteSpace(department) ? null : department.Trim()
            });
    }

    public async Task<List<UserInvitationItemDto>> FetchMyInvitationsAsync(Guid userId)
    {
        var query = $"organization_members?user_id=eq.{userId}&status=eq.pending" +
                    $"&select=id,role,status,title,invited_at,organization:organizations(id,name,tagline,avatar_path,industry,city)" +
                    $"&order=invited_at.desc";

        var list = await _supabase.GetListAsync<OrganizationMember>(query);

        return list.Select(m => new UserInvitationItemDto
        {
            Id = m.Id,
            Role = m.Role,
            Status = m.Status,
            Title = m.Title,
            InvitedAt = m.InvitedAt,
            Organization = m.Organization != null ? new UserInvitationOrgDto
            {
                Id = m.Organization.Id,
                Name = m.Organization.Name,
                Tagline = m.Organization.Tagline,
                AvatarPath = m.Organization.AvatarPath,
                Industry = m.Organization.Industry,
                City = m.Organization.City
            } : null
        }).ToList();
    }

    public async Task AcceptInvitationAsync(Guid membershipId)
    {
        await _supabase.UpdateAsync<OrganizationMember>(
            $"organization_members?id=eq.{membershipId}",
            new
            {
                status = "active",
                joined_at = DateTime.UtcNow
            });
    }

    public async Task DeclineInvitationAsync(Guid membershipId)
    {
        await _supabase.UpdateAsync<OrganizationMember>(
            $"organization_members?id=eq.{membershipId}",
            new
            {
                status = "rejected",
                rejection_reason = "Declined by invitee"
            });
    }

    private static OrgMemberDto MapToMemberDto(OrganizationMember m)
    {
        return new OrgMemberDto
        {
            Id = m.Id,
            OrganizationId = m.OrganizationId,
            UserId = m.UserId,
            Role = m.Role,
            Status = m.Status,
            Title = m.Title,
            Department = m.Department,
            JoinedAt = m.JoinedAt,
            InvitedAt = m.InvitedAt,
            InvitedBy = m.InvitedBy,
            RevokedAt = m.RevokedAt,
            RevokedBy = m.RevokedBy,
            RejectionReason = m.RejectionReason,
            User = m.User != null ? new OrgMemberUserDto
            {
                Id = m.User.Id,
                Email = m.User.Email,
                FirstName = m.User.FirstName,
                LastName = m.User.LastName,
                DisplayName = m.User.DisplayName,
                AvatarPath = m.User.AvatarPath
            } : null,
            Inviter = m.Inviter != null ? new OrgMemberUserDto
            {
                Id = m.Inviter.Id,
                Email = m.Inviter.Email,
                FirstName = m.Inviter.FirstName,
                LastName = m.Inviter.LastName,
                DisplayName = m.Inviter.DisplayName,
                AvatarPath = m.Inviter.AvatarPath
            } : null
        };
    }
}

