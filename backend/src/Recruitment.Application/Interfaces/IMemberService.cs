using Recruitment.Application.DTOs;

namespace Recruitment.Application.Interfaces;

public interface IMemberService
{
    Task<List<OrgMemberDto>> FetchMembersAsync(Guid organizationId);
    Task InviteMemberAsync(InviteMemberRequest request, Guid inviterId);
    Task AcceptMemberAsync(Guid memberId);
    Task RejectMemberAsync(Guid memberId, string? reason);
    Task RevokeMemberAsync(Guid memberId, Guid revokedByUserId);
    Task ReinviteMemberAsync(Guid memberId);
    Task DeleteMemberRecordAsync(Guid memberId);
    Task UpdateMemberRoleAsync(Guid memberId, string newRole);
    Task UpdateMemberDetailsAsync(Guid memberId, string? title, string? department);
    Task<List<UserInvitationItemDto>> FetchMyInvitationsAsync(Guid userId);
    Task AcceptInvitationAsync(Guid membershipId);
    Task DeclineInvitationAsync(Guid membershipId);
}

