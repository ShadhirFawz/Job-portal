import { api } from '@/lib/api'
import type {
  OrgMember,
  MemberRole,
  InviteMemberInput,
  UserInvitationItem,
} from '@/types/member'

export const memberService = {
  /**
   * Fetch all members of an organization with attached user & inviter profiles via .NET backend API.
   */
  async fetchMembers(organizationId: string): Promise<OrgMember[]> {
    if (!organizationId) return []
    try {
      return await api.get<OrgMember[]>(`members/organization/${organizationId}`)
    } catch (error: any) {
      console.error('Error fetching members via API:', error.message)
      return []
    }
  },

  /**
   * Invite a user to the organization by email via .NET backend API.
   */
  async inviteMember(input: InviteMemberInput): Promise<void> {
    const trimmedEmail = input.email.trim().toLowerCase()
    if (!trimmedEmail) {
      throw new Error('Please enter a valid email address.')
    }

    await api.post<{ success: boolean; message?: string }>(
      'members/invite',
      {
        organizationId: input.organizationId,
        email: trimmedEmail,
        role: input.role,
        title: input.title?.trim() || null,
      },
      { inviterId: input.inviterId }
    )
  },

  /**
   * Accept an invitation on behalf of member or approve pending candidate via .NET backend API.
   */
  async acceptMember(memberId: string): Promise<void> {
    await api.post<{ success: boolean }>(`members/${memberId}/accept`)
  },

  /**
   * Reject a member's invitation or application via .NET backend API.
   */
  async rejectMember(memberId: string, reason?: string): Promise<void> {
    await api.post<{ success: boolean }>(`members/${memberId}/reject`, {
      reason: reason?.trim() || 'Declined by administrator',
    })
  },

  /**
   * Revoke an active or pending member via .NET backend API.
   */
  async revokeMember(memberId: string, revokedByUserId: string): Promise<void> {
    await api.post<{ success: boolean }>(
      `members/${memberId}/revoke`,
      {},
      { revokedByUserId }
    )
  },

  /**
   * Resend an invitation via .NET backend API.
   */
  async reinviteMember(memberId: string): Promise<void> {
    await api.post<{ success: boolean }>(`members/${memberId}/reinvite`)
  },

  /**
   * Permanently delete a member record from the organization via .NET backend API.
   */
  async deleteMemberRecord(memberId: string): Promise<void> {
    await api.delete<{ success: boolean }>(`members/${memberId}`)
  },

  /**
   * Change member role between org_admin and org_staff via .NET backend API.
   */
  async updateMemberRole(memberId: string, newRole: MemberRole): Promise<void> {
    await api.put<{ success: boolean }>(`members/${memberId}/role`, { role: newRole })
  },

  /**
   * Update member title and department via .NET backend API.
   */
  async updateMemberDetails(
    memberId: string,
    title: string | null,
    department: string | null
  ): Promise<void> {
    await api.put<{ success: boolean }>(`members/${memberId}/details`, {
      title: title?.trim() || null,
      department: department?.trim() || null,
    })
  },

  /**
   * Fetch all pending invitations for a specific user via .NET backend API.
   */
  async fetchMyInvitations(userId: string): Promise<UserInvitationItem[]> {
    if (!userId) return []
    return await api.get<UserInvitationItem[]>(`members/invitations/user/${userId}`)
  },

  /**
   * Accept an invitation received by the current user via .NET backend API.
   */
  async acceptInvitation(membershipId: string): Promise<void> {
    await api.post<{ success: boolean }>(`members/invitations/${membershipId}/accept`)
  },

  /**
   * Decline an invitation received by the current user via .NET backend API.
   */
  async declineInvitation(membershipId: string): Promise<void> {
    await api.post<{ success: boolean }>(`members/invitations/${membershipId}/decline`)
  },
}
