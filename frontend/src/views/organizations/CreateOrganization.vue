<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { organizationService } from '@/services/organization.service'
import { profileService } from '@/services/profile.service'
import { uploadOrganizationImage } from '@/composables/useOrganizationImage'

const router = useRouter()
const auth = useAuthStore()

const loading = ref(false)
const errorMsg = ref('')

// Form
const name = ref('')
const legalName = ref('')
const officialEmail = ref('')
const tagline = ref('')
const description = ref('')
const websiteUrl = ref('')
const phone = ref('')
const industry = ref('')
const companySize = ref('')
const foundedYear = ref<number | null>(null)
const hasPhysicalOffice = ref(false)
const countryCode = ref('')
const city = ref('')
const addressLine1 = ref('')

// Images
const avatarFile = ref<File | null>(null)
const coverFile = ref<File | null>(null)
const avatarPreview = ref<string | null>(null)
const coverPreview = ref<string | null>(null)

function onAvatarChange(e: Event) {
  const file = (e.target as HTMLInputElement).files?.[0]
  if (file) {
    avatarFile.value = file
    avatarPreview.value = URL.createObjectURL(file)
  }
}

function onCoverChange(e: Event) {
  const file = (e.target as HTMLInputElement).files?.[0]
  if (file) {
    coverFile.value = file
    coverPreview.value = URL.createObjectURL(file)
  }
}

async function handleCreate() {
  if (!auth.user) return

  loading.value = true
  errorMsg.value = ''

  try {
    // 1. Create organization & auto-add creator as org_admin
    const org = await organizationService.createOrganization(
      {
        name: name.value.trim(),
        legal_name: legalName.value.trim() || null,
        official_email: officialEmail.value.trim().toLowerCase(),
        tagline: tagline.value.trim() || null,
        description: description.value.trim() || null,
        website_url: websiteUrl.value.trim() || null,
        phone: phone.value.trim() || null,
        industry: industry.value.trim() || null,
        company_size: companySize.value || null,
        founded_year: foundedYear.value,
        has_physical_office: hasPhysicalOffice.value,
        country_code: countryCode.value.trim() || null,
        city: city.value.trim() || null,
        address_line1: addressLine1.value.trim() || null,
      },
      auth.user.id
    )

    // 2. Upload images (now that we have org.id)
    let avatarPath: string | null = null
    let coverPath: string | null = null

    if (avatarFile.value) {
      avatarPath = await uploadOrganizationImage(org.id, avatarFile.value, 'avatar')
    }
    if (coverFile.value) {
      coverPath = await uploadOrganizationImage(org.id, coverFile.value, 'cover')
    }

    // 3. Update organization with image paths + generate slug
    await organizationService.updateOrganization(org.id, {
      avatar_path: avatarPath,
      cover_path: coverPath,
    })

    // 4. Promote user role to org_admin (if candidate)
    if (auth.user.role === 'candidate') {
      await profileService.updateUserProfile(auth.user.id, { role: 'org_admin' } as any)
      await auth.fetchUserProfile() // refresh local state
    }

    // Success → redirect to company dashboard
    router.push(`/organizations/${org.id}`)

  } catch (err: any) {
    console.error(err)
    errorMsg.value = err.message || 'Failed to create organization'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="min-h-screen bg-gray-50 py-10 px-4">
    <div class="max-w-3xl mx-auto">
      <div class="mb-8">
        <h1 class="text-3xl font-bold text-gray-900">Create a Company</h1>
        <p class="mt-2 text-gray-600">
          Set up your organization profile to start posting jobs and managing recruitment.
        </p>
      </div>

      <form @submit.prevent="handleCreate" class="bg-white shadow rounded-lg overflow-hidden">
        <!-- Cover + Avatar -->
        <div class="h-40 bg-gradient-to-r from-indigo-500 to-purple-600 relative">
          <img v-if="coverPreview" :src="coverPreview" class="w-full h-full object-cover" />
          <label class="absolute bottom-3 right-3 bg-white/90 hover:bg-white text-xs px-3 py-1.5 rounded cursor-pointer shadow">
            Upload Cover
            <input type="file" accept="image/*" class="hidden" @change="onCoverChange" />
          </label>
        </div>

        <div class="px-6 -mt-10 flex items-end gap-4">
          <div class="relative">
            <div class="h-20 w-20 rounded-full ring-4 ring-white bg-gray-200 overflow-hidden">
              <img v-if="avatarPreview" :src="avatarPreview" class="h-full w-full object-cover" />
            </div>
            <label class="absolute bottom-0 right-0 bg-indigo-600 text-white p-1 rounded-full cursor-pointer">
              <svg xmlns="http://www.w3.org/2000/svg" class="h-3.5 w-3.5" viewBox="0 0 20 20" fill="currentColor">
                <path d="M13.586 3.586a2 2 0 112.828 2.828l-.793.793-2.828-2.828.793-.793zM11.379 5.793L3 14.172V17h2.828l8.38-8.379-2.83-2.828z" />
              </svg>
              <input type="file" accept="image/*" class="hidden" @change="onAvatarChange" />
            </label>
          </div>
        </div>

        <div class="px-6 py-8 space-y-6">
          <div v-if="errorMsg" class="rounded-md bg-red-50 p-4 text-sm text-red-700">
            {{ errorMsg }}
          </div>

          <!-- Basic Info -->
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-6">
            <div class="sm:col-span-2">
              <label class="block text-sm font-medium text-gray-700">Company Name *</label>
              <input v-model="name" required type="text"
                class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm" />
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700">Legal Name</label>
              <input v-model="legalName" type="text"
                class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm" />
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700">Official Company Email *</label>
              <input v-model="officialEmail" required type="email"
                class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm" />
              <p class="mt-1 text-xs text-gray-500">Must be a company domain email. Verification required.</p>
            </div>
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700">Tagline</label>
            <input v-model="tagline" type="text" placeholder="Short one-liner about your company"
              class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm" />
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700">Description</label>
            <textarea v-model="description" rows="4"
              class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"></textarea>
          </div>

          <!-- Extra Details -->
          <div class="grid grid-cols-1 sm:grid-cols-3 gap-6">
            <div>
              <label class="block text-sm font-medium text-gray-700">Industry</label>
              <input v-model="industry" type="text"
                class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700">Company Size</label>
              <select v-model="companySize"
                class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm">
                <option value="">Select</option>
                <option value="1-10">1-10</option>
                <option value="11-50">11-50</option>
                <option value="51-200">51-200</option>
                <option value="201-500">201-500</option>
                <option value="501-1000">501-1000</option>
                <option value="1000+">1000+</option>
              </select>
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700">Founded Year</label>
              <input v-model.number="foundedYear" type="number" min="1800" max="2026"
                class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm" />
            </div>
          </div>

          <!-- Contact -->
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-6">
            <div>
              <label class="block text-sm font-medium text-gray-700">Website</label>
              <input v-model="websiteUrl" type="url"
                class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700">Phone</label>
              <input v-model="phone" type="tel"
                class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm" />
            </div>
          </div>

          <!-- Location -->
          <div class="border-t pt-6">
            <div class="flex items-center gap-2 mb-4">
              <input id="hasOffice" v-model="hasPhysicalOffice" type="checkbox" class="rounded border-gray-300" />
              <label for="hasOffice" class="text-sm font-medium text-gray-700">This company has a physical office</label>
            </div>

            <div v-if="hasPhysicalOffice" class="grid grid-cols-1 sm:grid-cols-2 gap-6">
              <div>
                <label class="block text-sm font-medium text-gray-700">Country Code</label>
                <input v-model="countryCode" type="text" maxlength="2" placeholder="IN"
                  class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm" />
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700">City</label>
                <input v-model="city" type="text"
                  class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm" />
              </div>
              <div class="sm:col-span-2">
                <label class="block text-sm font-medium text-gray-700">Address</label>
                <input v-model="addressLine1" type="text"
                  class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm" />
              </div>
            </div>
          </div>

          <div class="pt-4">
            <button type="submit" :disabled="loading"
              class="w-full flex justify-center py-3 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50">
              {{ loading ? 'Creating Company...' : 'Create Company' }}
            </button>
          </div>
        </div>
      </form>
    </div>
  </div>
</template>