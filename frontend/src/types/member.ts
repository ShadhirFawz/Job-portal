export type MemberRole = 'org_admin' | 'org_staff'

export type MemberStatus = 'active' | 'pending' | 'revoked' | 'rejected'

export interface OrgMemberUser {
  id: string
  email: string
  first_name: string | null
  last_name: string | null
  display_name: string | null
  avatar_path: string | null
}

export interface OrgMemberInviter {
  id: string
  email: string
  display_name: string | null
  first_name: string | null
  last_name?: string | null
}

export interface OrgMember {
  id: string
  organization_id: string
  user_id: string
  role: MemberRole
  status: MemberStatus
  title: string | null
  department: string | null
  joined_at: string | null
  invited_at: string | null
  invited_by: string | null
  rejection_reason?: string | null
  created_at?: string
  updated_at?: string
  user?: OrgMemberUser | null
  inviter?: OrgMemberInviter | null
}

export interface InviteMemberInput {
  organizationId: string
  email: string
  role: MemberRole
  title?: string | null
  inviterId: string
}

export interface UserInvitationItem {
  id: string
  role: MemberRole
  status: MemberStatus
  title: string | null
  invited_at: string | null
  organization: {
    id: string
    name: string
    tagline: string | null
    avatar_path: string | null
    industry: string | null
    city: string | null
  }
}

