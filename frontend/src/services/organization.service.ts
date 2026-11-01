import { api } from '@/lib/api'
import type {
  Organization,
  OrganizationCreateInput,
  OrganizationUpdateInput,
} from '@/types/organization'

export const organizationService = {
  /**
   * Fetch an organization by ID via .NET backend API.
   */
  async getOrganization(id: string): Promise<Organization | null> {
    if (!id) return null
    try {
      return await api.get<Organization>(`organizations/${id}`)
    } catch (error) {
      console.error('Error fetching organization via API:', error)
      return null
    }
  },

  /**
   * Fetch all organizations where the user is an active member via .NET backend API.
   */
  async getMyOrganizations(userId: string): Promise<Organization[]> {
    if (!userId) return []
    try {
      return await api.get<Organization[]>(`organizations/user/${userId}`)
    } catch (error) {
      console.error('Error fetching user organizations via API:', error)
      return []
    }
  },

  /**
   * Check if a user is an OrgAdmin via .NET backend API.
   */
  async checkIsOrgAdmin(
    orgId: string,
    userId: string,
    userRole?: string,
    orgCreatedBy?: string
  ): Promise<boolean> {
    if (orgCreatedBy && orgCreatedBy === userId) return true
    if (userRole === 'super_admin') return true

    try {
      const response = await api.get<{ isAdmin: boolean }>(
        `organizations/${orgId}/is-admin`,
        {
          userId,
          userRole,
          orgCreatedBy,
        }
      )
      return response.isAdmin
    } catch (error) {
      console.error('Error checking org_admin permissions via API:', error)
      return false
    }
  },

  /**
   * Check if a user is an OrgAdmin or OrgStaff via .NET backend API.
   */
  async checkIsOrgAdminOrStaff(
    orgId: string,
    userId: string,
    userRole?: string,
    orgCreatedBy?: string
  ): Promise<boolean> {
    if (orgCreatedBy && orgCreatedBy === userId) return true
    if (userRole === 'super_admin') return true

    try {
      const response = await api.get<{ isStaff: boolean }>(
        `organizations/${orgId}/is-staff`,
        {
          userId,
          userRole,
          orgCreatedBy,
        }
      )
      return response.isStaff
    } catch (error) {
      console.error('Error checking org permissions via API:', error)
      return false
    }
  },

  /**
   * Create a new organization and assign the creator as active org_admin via .NET backend API.
   */
  async createOrganization(
    input: OrganizationCreateInput,
    userId: string
  ): Promise<Organization> {
    return await api.post<Organization>('organizations', input, { userId })
  },

  /**
   * Update an existing organization via .NET backend API.
   */
  async updateOrganization(
    id: string,
    payload: OrganizationUpdateInput
  ): Promise<Organization> {
    return await api.put<Organization>(`organizations/${id}`, payload)
  },

  /**
   * Generate an official email verification link via .NET backend API.
   */
  async createEmailVerificationToken(
    orgId: string,
    email: string,
    userId: string
  ): Promise<{ token: string; verifyUrl: string }> {
    return await api.post<{ token: string; verifyUrl: string }>(
      `organizations/${orgId}/email-verification`,
      {
        email,
        origin: window.location.origin,
      },
      { userId }
    )
  },

  /**
   * Verify an official email token via .NET backend API.
   */
  async verifyEmailToken(token: string): Promise<{
    organizationId: string
    organizationName: string
    officialEmail: string
  }> {
    return await api.post<{
      organizationId: string
      organizationName: string
      officialEmail: string
    }>('organizations/verify-email', { token })
  },
}
