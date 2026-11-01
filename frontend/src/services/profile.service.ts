import { api } from '@/lib/api'
import type { AppUser, ProfileUpdateInput } from '@/types/user'

export const profileService = {
  /**
   * Fetch user profile via .NET backend API.
   */
  async getUserProfile(userId: string): Promise<AppUser | null> {
    try {
      return await api.get<AppUser>(`profiles/${userId}`)
    } catch (error) {
      console.error('Error fetching user profile via API:', error)
      return null
    }
  },

  /**
   * Update a user profile via .NET backend API.
   */
  async updateUserProfile(userId: string, input: ProfileUpdateInput): Promise<AppUser> {
    return await api.put<AppUser>(`profiles/${userId}`, input)
  },
}
