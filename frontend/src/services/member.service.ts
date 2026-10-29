import { supabase } from '@/lib/supabase'
import type {
  OrgMember,
  MemberRole,
  InviteMemberInput,
  UserInvitationItem,
} from '@/types/member'

export const memberService = {
  /**
   * Fetch all members of an organization with attached user & inviter profiles.
   */
  async fetchMembers(organizationId: string): Promise<OrgMember[]> {
    if (!organizationId) return []

    let { data, error } = await supabase
      .from('organization_members')
      .select(`
        id, organization_id, user_id, role, status, title, department,
        joined_at, invited_at, invited_by,
        user:users!user_id ( id, email, first_name, last_name, display_name, avatar_path ),
        inviter:users!invited_by ( id, email, display_name, first_name, last_name )
      `)
      .eq('organization_id', organizationId)
      .order('created_at', { ascending: true })

    // Fallback if foreign key join is restricted by RLS or Supabase schema cache
    if (error) {
      console.warn('Direct FK member join failed, using user-mapping fallback:', error.message)
      const { data: rawMembers, error: rawError } = await supabase
        .from('organization_members')
        .select('*')
        .eq('organization_id', organizationId)
        .order('created_at', { ascending: true })

      if (rawError) throw rawError

      if (rawMembers && rawMembers.length > 0) {
        const userIds = [...new Set(rawMembers.map((m: any) => m.user_id).filter(Boolean))]
        const inviterIds = [...new Set(rawMembers.map((m: any) => m.invited_by).filter(Boolean))]
        const allIds = [...new Set([...userIds, ...inviterIds])]

        let usersMap = new Map<string, any>()
        if (allIds.length > 0) {
          const { data: usersData } = await supabase
            .from('users')
            .select('id, email, first_name, last_name, display_name, avatar_path')
            .in('id', allIds)

          usersMap = new Map((usersData || []).map((u: any) => [u.id, u]))
        }

        data = rawMembers.map((m: any) => ({
          ...m,
          user: usersMap.get(m.user_id) || null,
          inviter: usersMap.get(m.invited_by) || null,
        }))
      } else {
        data = []
      }
    }

    return (data || []) as unknown as OrgMember[]
  },

  /**
   * Invite a user to the organization by email.
   */
  async inviteMember(input: InviteMemberInput): Promise<void> {
    const trimmedEmail = input.email.trim().toLowerCase()
    if (!trimmedEmail) {
      throw new Error('Please enter a valid email address.')
    }

    // 1. Find user by email (try RPC first, fallback to ilike query)
    let user: { id: string; email: string } | null = null

    const { data: rpcData, error: rpcError } = await supabase
      .rpc('find_user_by_email', { search_email: trimmedEmail })

    if (!rpcError && rpcData && rpcData.length > 0) {
      user = rpcData[0]
    } else {
      const { data: userData, error: userError } = await supabase
        .from('users')
        .select('id, email')
        .ilike('email', trimmedEmail)
        .maybeSingle()

      if (userError) throw userError
      user = userData
    }

    if (!user) {
      throw new Error('No user found with this email. They must register on the platform first.')
    }

    // 2. Check if user is already in this organization
    const { data: existing, error: existingError } = await supabase
      .from('organization_members')
      .select('id, status, role')
      .eq('organization_id', input.organizationId)
      .eq('user_id', user.id)
      .maybeSingle()

    if (existingError) throw existingError

    if (existing) {
      if (existing.status === 'active') {
        throw new Error('This user is already an active member of this organization.')
      } else if (existing.status === 'pending') {
        throw new Error('An invitation is already pending for this user.')
      } else {
        // Re-invite if previously revoked or rejected
        const { error: updateError } = await supabase
          .from('organization_members')
          .update({
            status: 'pending',
            role: input.role,
            title: input.title?.trim() || null,
            invited_at: new Date().toISOString(),
            invited_by: input.inviterId,
            revoked_at: null,
            revoked_by: null,
            rejection_reason: null,
          })
          .eq('id', existing.id)

        if (updateError) throw updateError
        return
      }
    }

    // 3. Insert new membership record
    const { error: insertError } = await supabase
      .from('organization_members')
      .insert({
        organization_id: input.organizationId,
        user_id: user.id,
        role: input.role,
        status: 'pending',
        title: input.title?.trim() || null,
        invited_at: new Date().toISOString(),
        invited_by: input.inviterId,
      })

    if (insertError) throw insertError
  },

  /**
   * Accept an invitation on behalf of member or approve pending candidate.
   */
  async acceptMember(memberId: string): Promise<void> {
    const { error } = await supabase
      .from('organization_members')
      .update({
        status: 'active',
        joined_at: new Date().toISOString(),
      })
      .eq('id', memberId)

    if (error) throw error
  },

  /**
   * Reject a member's invitation or application.
   */
  async rejectMember(memberId: string, reason?: string): Promise<void> {
    const { error } = await supabase
      .from('organization_members')
      .update({
        status: 'rejected',
        rejection_reason: reason?.trim() || 'Declined by administrator',
      })
      .eq('id', memberId)

    if (error) throw error
  },

  /**
   * Revoke an active or pending member.
   */
  async revokeMember(memberId: string, revokedByUserId: string): Promise<void> {
    const { error } = await supabase
      .from('organization_members')
      .update({
        status: 'revoked',
        revoked_at: new Date().toISOString(),
        revoked_by: revokedByUserId,
      })
      .eq('id', memberId)

    if (error) throw error
  },

  /**
   * Resend an invitation.
   */
  async reinviteMember(memberId: string): Promise<void> {
    const { error } = await supabase
      .from('organization_members')
      .update({
        status: 'pending',
        invited_at: new Date().toISOString(),
        revoked_at: null,
        revoked_by: null,
        rejection_reason: null,
      })
      .eq('id', memberId)

    if (error) throw error
  },

  /**
   * Permanently delete a member record from the organization.
   */
  async deleteMemberRecord(memberId: string): Promise<void> {
    const { error } = await supabase
      .from('organization_members')
      .delete()
      .eq('id', memberId)

    if (error) throw error
  },

  /**
   * Change member role between org_admin and org_staff.
   */
  async updateMemberRole(memberId: string, newRole: MemberRole): Promise<void> {
    const { error } = await supabase
      .from('organization_members')
      .update({ role: newRole })
      .eq('id', memberId)

    if (error) throw error
  },

  /**
   * Update member title and department.
   */
  async updateMemberDetails(
    memberId: string,
    title: string | null,
    department: string | null
  ): Promise<void> {
    const { error } = await supabase
      .from('organization_members')
      .update({
        title: title?.trim() || null,
        department: department?.trim() || null,
      })
      .eq('id', memberId)

    if (error) throw error
  },

  /**
   * Fetch all pending invitations for a specific user.
   */
  async fetchMyInvitations(userId: string): Promise<UserInvitationItem[]> {
    const { data, error } = await supabase
      .from('organization_members')
      .select(`
        id,
        role,
        status,
        title,
        invited_at,
        organization:organizations (
          id,
          name,
          tagline,
          avatar_path,
          industry,
          city
        )
      `)
      .eq('user_id', userId)
      .eq('status', 'pending')
      .order('invited_at', { ascending: false })

    if (error) throw error
    return (data || []) as unknown as UserInvitationItem[]
  },

  /**
   * Accept an invitation received by the current user.
   */
  async acceptInvitation(membershipId: string): Promise<void> {
    const { error } = await supabase
      .from('organization_members')
      .update({
        status: 'active',
        joined_at: new Date().toISOString(),
      })
      .eq('id', membershipId)

    if (error) throw error
  },

  /**
   * Decline an invitation received by the current user.
   */
  async declineInvitation(membershipId: string): Promise<void> {
    const { error } = await supabase
      .from('organization_members')
      .update({
        status: 'rejected',
        rejection_reason: 'Declined by invitee',
      })
      .eq('id', membershipId)

    if (error) throw error
  },
}

