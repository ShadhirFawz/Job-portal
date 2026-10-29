import { supabase } from '@/lib/supabase'
import type {
  Organization,
  OrganizationCreateInput,
  OrganizationUpdateInput,
} from '@/types/organization'

export const organizationService = {
  /**
   * Fetch an organization by ID.
   */
  async getOrganization(id: string): Promise<Organization | null> {
    const { data, error } = await supabase
      .from('organizations')
      .select('*')
      .eq('id', id)
      .maybeSingle()

    if (error) {
      console.error('Error fetching organization:', error)
      return null
    }
    return data as Organization
  },

  /**
   * Fetch all organizations where the user is an active member.
   */
  async getMyOrganizations(userId: string): Promise<Organization[]> {
    const { data, error } = await supabase
      .from('organization_members')
      .select(`
        role,
        status,
        organization:organizations (*)
      `)
      .eq('user_id', userId)
      .eq('status', 'active')

    if (error) {
      console.error('Error fetching user organizations:', error)
      return []
    }

    return (data || [])
      .map((item: any) => item.organization)
      .filter(Boolean) as Organization[]
  },

  /**
   * Check if a user is an OrgAdmin (creator, super_admin, or active org_admin member).
   */
  async checkIsOrgAdmin(
    orgId: string,
    userId: string,
    userRole?: string,
    orgCreatedBy?: string
  ): Promise<boolean> {
    if (orgCreatedBy && orgCreatedBy === userId) return true
    if (userRole === 'super_admin') return true

    const { data, error } = await supabase
      .from('organization_members')
      .select('role, status')
      .eq('organization_id', orgId)
      .eq('user_id', userId)
      .eq('status', 'active')
      .maybeSingle()

    if (error) {
      console.error('Error checking org_admin permissions:', error)
      return false
    }

    return data?.role === 'org_admin'
  },

  /**
   * Create a new organization and assign the creator as active org_admin.
   */
  async createOrganization(
    input: OrganizationCreateInput,
    userId: string
  ): Promise<Organization> {
    const { data: org, error: orgError } = await supabase
      .from('organizations')
      .insert({
        name: input.name.trim(),
        legal_name: input.legal_name?.trim() || null,
        official_email: input.official_email.trim().toLowerCase(),
        tagline: input.tagline?.trim() || null,
        description: input.description?.trim() || null,
        website_url: input.website_url?.trim() || null,
        phone: input.phone?.trim() || null,
        industry: input.industry?.trim() || null,
        company_size: input.company_size || null,
        founded_year: input.founded_year || null,
        has_physical_office: !!input.has_physical_office,
        country_code: input.country_code?.trim() || null,
        city: input.city?.trim() || null,
        address_line1: input.address_line1?.trim() || null,
        created_by: userId,
        status: 'pending_verification',
      })
      .select()
      .single()

    if (orgError) throw orgError

    // Add creator as org_admin in organization_members
    const { error: memberError } = await supabase
      .from('organization_members')
      .insert({
        organization_id: org.id,
        user_id: userId,
        role: 'org_admin',
        status: 'active',
        joined_at: new Date().toISOString(),
        title: 'Founder / Administrator',
      })

    if (memberError) {
      console.warn('Could not auto-add creator to organization_members:', memberError)
    }

    return org as Organization
  },

  /**
   * Update an existing organization.
   */
  async updateOrganization(
    id: string,
    payload: OrganizationUpdateInput
  ): Promise<Organization> {
    const { data, error } = await supabase
      .from('organizations')
      .update(payload)
      .eq('id', id)
      .select()
      .single()

    if (error) throw error
    return data as Organization
  },

  /**
   * Generate an official email verification link.
   */
  async createEmailVerificationToken(
    orgId: string,
    email: string,
    userId: string
  ): Promise<{ token: string; verifyUrl: string }> {
    const token = crypto.randomUUID() + '-' + crypto.randomUUID()
    const expiresAt = new Date()
    expiresAt.setHours(expiresAt.getHours() + 24)

    // Clear previous pending tokens
    await supabase
      .from('organization_email_verifications')
      .delete()
      .eq('organization_id', orgId)
      .is('verified_at', null)

    const { error } = await supabase
      .from('organization_email_verifications')
      .insert({
        organization_id: orgId,
        email: email.trim().toLowerCase(),
        token,
        expires_at: expiresAt.toISOString(),
        created_by: userId,
      })

    if (error) throw error

    const verifyUrl = `${window.location.origin}/organizations/verify-email?token=${token}`
    return { token, verifyUrl }
  },

  /**
   * Verify an official email token.
   */
  async verifyEmailToken(token: string): Promise<{
    organizationId: string
    organizationName: string
    officialEmail: string
  }> {
    // 1. Fetch token record
    const { data: verification, error } = await supabase
      .from('organization_email_verifications')
      .select('*, organization:organizations(id, name, official_email)')
      .eq('token', token)
      .is('verified_at', null)
      .single()

    if (error || !verification) {
      throw new Error('Invalid or already used verification link.')
    }

    // 2. Check expiration
    if (new Date(verification.expires_at) < new Date()) {
      throw new Error('This verification link has expired. Please request a new one.')
    }

    // 3. Mark token verified
    const { error: tokenErr } = await supabase
      .from('organization_email_verifications')
      .update({ verified_at: new Date().toISOString() })
      .eq('id', verification.id)

    if (tokenErr) throw tokenErr

    // 4. Update organization official_email_verified
    const { error: orgErr } = await supabase
      .from('organizations')
      .update({
        official_email_verified: true,
        status: 'active',
        verified_at: new Date().toISOString(),
      })
      .eq('id', verification.organization_id)

    if (orgErr) throw orgErr

    return {
      organizationId: verification.organization_id,
      organizationName: verification.organization?.name || '',
      officialEmail: verification.organization?.official_email || '',
    }
  },
}

