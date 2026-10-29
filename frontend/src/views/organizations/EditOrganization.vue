<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { toast } from 'vue-sonner'
import {
  ArrowLeft,
  Building2,
  Camera,
  Check,
  Globe,
  Loader2,
  Mail,
  MapPin,
  Phone,
  Trash2,
  Upload,
  AlertCircle,
  ShieldAlert,
} from 'lucide-vue-next'
import { supabase } from '@/lib/supabase'
import { useAuthStore } from '@/stores/auth'
import { useOrganizationsStore } from '@/stores/organizations'
import {
  uploadOrganizationImage,
  deleteOrganizationImage,
  getOrganizationImageUrl,
} from '@/composables/useOrganizationImage'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const orgStore = useOrganizationsStore()

const orgId = route.params.id as string

const pageLoading = ref(true)
const saving = ref(false)
const authorized = ref(false)
const errorMsg = ref('')

// Form State
const name = ref('')
const legalName = ref('')
const tagline = ref('')
const description = ref('')
const officialEmail = ref('')
const originalOfficialEmail = ref('')
const supportEmail = ref('')
const phone = ref('')
const websiteUrl = ref('')

const industry = ref('')
const companySize = ref('')
const foundedYear = ref<number | null>(null)
const companyType = ref('')

const hasPhysicalOffice = ref(false)
const countryCode = ref('')
const stateProvince = ref('')
const city = ref('')
const postalCode = ref('')
const addressLine1 = ref('')
const addressLine2 = ref('')

const linkedinUrl = ref('')
const twitterUrl = ref('')
const facebookUrl = ref('')
const githubUrl = ref('')
const glassdoorUrl = ref('')

// Images state
const currentAvatarPath = ref<string | null>(null)
const currentCoverPath = ref<string | null>(null)

const avatarFile = ref<File | null>(null)
const coverFile = ref<File | null>(null)
const avatarPreview = ref<string | null>(null)
const coverPreview = ref<string | null>(null)
const removeAvatar = ref(false)
const removeCover = ref(false)

const companyInitial = computed(() => {
  return (name.value || 'O').trim().charAt(0).toUpperCase()
})

onMounted(async () => {
  await loadAndAuthorize()
})

async function loadAndAuthorize() {
  pageLoading.value = true
  errorMsg.value = ''

  if (!auth.user) {
    toast.error('You must be signed in to access this page.')
    router.replace(`/organizations/${orgId}`)
    return
  }

  try {
    // 1. Fetch organization data
    const { data: org, error: orgErr } = await supabase
      .from('organizations')
      .select('*')
      .eq('id', orgId)
      .single()

    if (orgErr || !org) {
      toast.error('Organization not found.')
      router.replace('/organizations')
      return
    }

    // 2. Check authorization: must be creator, super_admin, or active org_admin member
    let isAllowed = false

    if (org.created_by === auth.user.id || auth.user.role === 'super_admin') {
      isAllowed = true
    } else {
      const { data: member } = await supabase
        .from('organization_members')
        .select('role, status')
        .eq('organization_id', orgId)
        .eq('user_id', auth.user.id)
        .eq('status', 'active')
        .maybeSingle()

      if (member?.role === 'org_admin') {
        isAllowed = true
      }
    }

    if (!isAllowed) {
      toast.error('You do not have permission to edit this organization.')
      router.replace(`/organizations/${orgId}`)
      return
    }

    authorized.value = true

    // 3. Populate form
    name.value = org.name || ''
    legalName.value = org.legal_name || ''
    tagline.value = org.tagline || ''
    description.value = org.description || ''
    officialEmail.value = org.official_email || ''
    originalOfficialEmail.value = org.official_email || ''
    supportEmail.value = org.support_email || ''
    phone.value = org.phone || ''
    websiteUrl.value = org.website_url || ''

    industry.value = org.industry || ''
    companySize.value = org.company_size || ''
    foundedYear.value = org.founded_year || null
    companyType.value = org.company_type || ''

    hasPhysicalOffice.value = !!org.has_physical_office
    countryCode.value = org.country_code || ''
    stateProvince.value = org.state_province || ''
    city.value = org.city || ''
    postalCode.value = org.postal_code || ''
    addressLine1.value = org.address_line1 || ''
    addressLine2.value = org.address_line2 || ''

    linkedinUrl.value = org.linkedin_url || ''
    twitterUrl.value = org.twitter_url || ''
    facebookUrl.value = org.facebook_url || ''
    githubUrl.value = org.github_url || ''
    glassdoorUrl.value = org.glassdoor_url || ''

    currentAvatarPath.value = org.avatar_path || null
    currentCoverPath.value = org.cover_path || null

    if (org.avatar_path) {
      avatarPreview.value = getOrganizationImageUrl(org.avatar_path)
    }
    if (org.cover_path) {
      coverPreview.value = getOrganizationImageUrl(org.cover_path)
    }
  } catch (err: any) {
    console.error('Failed to load organization:', err)
    errorMsg.value = err.message || 'Failed to load organization details.'
  } finally {
    pageLoading.value = false
  }
}

function onAvatarChange(e: Event) {
  const target = e.target as HTMLInputElement
  const file = target.files?.[0]
  if (file) {
    avatarFile.value = file
    avatarPreview.value = URL.createObjectURL(file)
    removeAvatar.value = false
  }
}

function handleRemoveAvatar() {
  avatarFile.value = null
  avatarPreview.value = null
  removeAvatar.value = true
}

function onCoverChange(e: Event) {
  const target = e.target as HTMLInputElement
  const file = target.files?.[0]
  if (file) {
    coverFile.value = file
    coverPreview.value = URL.createObjectURL(file)
    removeCover.value = false
  }
}

function handleRemoveCover() {
  coverFile.value = null
  coverPreview.value = null
  removeCover.value = true
}

async function handleSave() {
  if (!name.value.trim()) {
    toast.error('Company Name is required')
    return
  }
  if (!officialEmail.value.trim()) {
    toast.error('Official Company Email is required')
    return
  }

  saving.value = true
  errorMsg.value = ''

  try {
    let finalAvatarPath = currentAvatarPath.value
    let finalCoverPath = currentCoverPath.value

    // Handle avatar upload / removal
    if (removeAvatar.value && !avatarFile.value) {
      if (currentAvatarPath.value) {
        await deleteOrganizationImage(currentAvatarPath.value)
      }
      finalAvatarPath = null
    } else if (avatarFile.value) {
      if (currentAvatarPath.value) {
        await deleteOrganizationImage(currentAvatarPath.value)
      }
      finalAvatarPath = await uploadOrganizationImage(orgId, avatarFile.value, 'avatar')
    }

    // Handle cover upload / removal
    if (removeCover.value && !coverFile.value) {
      if (currentCoverPath.value) {
        await deleteOrganizationImage(currentCoverPath.value)
      }
      finalCoverPath = null
    } else if (coverFile.value) {
      if (currentCoverPath.value) {
        await deleteOrganizationImage(currentCoverPath.value)
      }
      finalCoverPath = await uploadOrganizationImage(orgId, coverFile.value, 'cover')
    }

    // Prepare update payload
    const emailChanged = officialEmail.value.trim().toLowerCase() !== originalOfficialEmail.value.trim().toLowerCase()

    const updatePayload: Record<string, any> = {
      name: name.value.trim(),
      legal_name: legalName.value.trim() || null,
      tagline: tagline.value.trim() || null,
      description: description.value.trim() || null,
      official_email: officialEmail.value.trim().toLowerCase(),
      support_email: supportEmail.value.trim().toLowerCase() || null,
      phone: phone.value.trim() || null,
      website_url: websiteUrl.value.trim() || null,
      industry: industry.value.trim() || null,
      company_size: companySize.value || null,
      founded_year: foundedYear.value ? Number(foundedYear.value) : null,
      company_type: companyType.value || null,
      has_physical_office: hasPhysicalOffice.value,
      country_code: countryCode.value.trim().toUpperCase() || null,
      state_province: stateProvince.value.trim() || null,
      city: city.value.trim() || null,
      postal_code: postalCode.value.trim() || null,
      address_line1: addressLine1.value.trim() || null,
      address_line2: addressLine2.value.trim() || null,
      linkedin_url: linkedinUrl.value.trim() || null,
      twitter_url: twitterUrl.value.trim() || null,
      facebook_url: facebookUrl.value.trim() || null,
      github_url: githubUrl.value.trim() || null,
      glassdoor_url: glassdoorUrl.value.trim() || null,
      avatar_path: finalAvatarPath,
      cover_path: finalCoverPath,
      updated_at: new Date().toISOString(),
    }

    // If official email changed, reset verified status
    if (emailChanged) {
      updatePayload.official_email_verified = false
    }

    const { error: updateErr } = await supabase
      .from('organizations')
      .update(updatePayload)
      .eq('id', orgId)

    if (updateErr) throw updateErr

    // Refresh organization in store
    await orgStore.fetchOrganization(orgId)

    toast.success('Organization updated successfully!')
    router.push(`/organizations/${orgId}`)
  } catch (err: any) {
    console.error('Update failed:', err)
    errorMsg.value = err.message || 'Failed to update organization.'
    toast.error(errorMsg.value)
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <div class="min-h-screen bg-gray-50/70 py-8 px-4 sm:px-6 lg:px-8">
    <div class="max-w-4xl mx-auto">
      <!-- Loading State -->
      <div v-if="pageLoading" class="flex flex-col items-center justify-center py-24 text-gray-500">
        <Loader2 class="h-8 w-8 animate-spin text-indigo-600" />
        <p class="mt-3 text-sm">Verifying permissions and loading company details…</p>
      </div>

      <!-- Unauthorized State -->
      <div
        v-else-if="!authorized"
        class="bg-white rounded-2xl shadow-sm border border-red-200 p-8 text-center"
      >
        <ShieldAlert class="h-12 w-12 text-red-600 mx-auto mb-3" />
        <h2 class="text-xl font-bold text-gray-900">Access Denied</h2>
        <p class="text-gray-600 mt-2">Only organization administrators can edit this company profile.</p>
        <router-link
          :to="`/organizations/${orgId}`"
          class="mt-6 inline-flex items-center gap-2 px-4 py-2 bg-indigo-600 text-white rounded-lg text-sm font-medium hover:bg-indigo-700 transition"
        >
          <ArrowLeft class="h-4 w-4" /> Return to Organization
        </router-link>
      </div>

      <!-- Edit Form -->
      <div v-else class="space-y-6">
        <!-- Top Navigation & Heading -->
        <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
          <div>
            <router-link
              :to="`/organizations/${orgId}`"
              class="inline-flex items-center gap-1.5 text-sm font-medium text-gray-600 hover:text-gray-900 transition mb-2"
            >
              <ArrowLeft class="h-4 w-4" />
              Back to Company Profile
            </router-link>
            <h1 class="text-2xl sm:text-3xl font-bold text-gray-900">Edit Company Profile</h1>
            <p class="mt-1 text-sm text-gray-600">
              Manage company details, visual branding, contact information, and locations.
            </p>
          </div>

          <div class="flex items-center gap-3">
            <router-link
              :to="`/organizations/${orgId}`"
              class="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 transition"
            >
              Cancel
            </router-link>
            <button
              type="button"
              :disabled="saving"
              @click="handleSave"
              class="inline-flex items-center gap-2 px-5 py-2 text-sm font-semibold text-white bg-indigo-600 rounded-lg hover:bg-indigo-700 shadow-sm disabled:opacity-50 transition"
            >
              <Loader2 v-if="saving" class="h-4 w-4 animate-spin" />
              <Check v-else class="h-4 w-4" />
              {{ saving ? 'Saving…' : 'Save Changes' }}
            </button>
          </div>
        </div>

        <div v-if="errorMsg" class="rounded-xl bg-red-50 p-4 border border-red-200 text-sm text-red-700 flex items-start gap-3">
          <AlertCircle class="h-5 w-5 shrink-0 text-red-500 mt-0.5" />
          <span>{{ errorMsg }}</span>
        </div>

        <form @submit.prevent="handleSave" class="space-y-6">
          <!-- 1. BRANDING & MEDIA -->
          <div class="bg-white rounded-2xl shadow-sm border border-gray-200 overflow-hidden">
            <div class="p-6 border-b border-gray-100">
              <h2 class="text-lg font-semibold text-gray-900">Visual Branding</h2>
              <p class="text-sm text-gray-500 mt-0.5">Upload high-resolution images for your company cover and logo.</p>
            </div>

            <!-- Cover Image Box -->
            <div class="p-6 space-y-6">
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-2">Company Cover Banner</label>
                <div class="relative h-44 sm:h-52 rounded-2xl overflow-hidden bg-gradient-to-r from-indigo-600 via-purple-600 to-fuchsia-600 border border-gray-200 shadow-inner group">
                  <!-- Watermark when no image preview -->
                  <div
                    v-if="!coverPreview"
                    class="absolute inset-0 flex items-center justify-end pr-10 pointer-events-none select-none opacity-20"
                  >
                    <span class="text-9xl font-black text-white uppercase">{{ companyInitial }}</span>
                  </div>

                  <img
                    v-if="coverPreview"
                    :src="coverPreview"
                    class="w-full h-full object-cover"
                    alt="Cover preview"
                  />
                  <div class="absolute inset-0 bg-black/25 pointer-events-none"></div>

                  <!-- Cover Buttons Overlay -->
                  <div class="absolute bottom-3 right-3 flex items-center gap-2">
                    <label class="inline-flex items-center gap-1.5 bg-white/90 hover:bg-white text-gray-800 text-xs font-semibold px-3 py-1.5 rounded-lg cursor-pointer shadow-md backdrop-blur-xs transition">
                      <Camera class="h-3.5 w-3.5 text-gray-600" />
                      {{ coverPreview ? 'Change Cover' : 'Upload Cover' }}
                      <input type="file" accept="image/*" class="hidden" @change="onCoverChange" />
                    </label>
                    <button
                      v-if="coverPreview"
                      type="button"
                      @click="handleRemoveCover"
                      class="inline-flex items-center gap-1 bg-red-600/90 hover:bg-red-600 text-white text-xs font-semibold px-2.5 py-1.5 rounded-lg shadow-md backdrop-blur-xs transition"
                      title="Remove cover image"
                    >
                      <Trash2 class="h-3.5 w-3.5" />
                    </button>
                  </div>
                </div>
                <p class="text-xs text-gray-500 mt-2">Recommended: 1200×400px. JPG, PNG or WebP up to 5MB.</p>
              </div>

              <!-- Avatar / Logo Box -->
              <div class="pt-2 flex flex-col sm:flex-row items-start sm:items-center gap-6">
                <div class="relative">
                  <div class="h-24 w-24 rounded-2xl ring-4 ring-white bg-gradient-to-br from-indigo-600 to-purple-700 overflow-hidden shadow-md flex items-center justify-center">
                    <img
                      v-if="avatarPreview"
                      :src="avatarPreview"
                      class="h-full w-full object-cover"
                      alt="Avatar preview"
                    />
                    <span v-else class="text-3xl font-bold text-white select-none">
                      {{ companyInitial }}
                    </span>
                  </div>
                </div>

                <div class="space-y-2">
                  <span class="block text-sm font-medium text-gray-700">Company Logo / Avatar</span>
                  <div class="flex flex-wrap items-center gap-2">
                    <label class="inline-flex items-center gap-1.5 bg-white border border-gray-300 hover:bg-gray-50 text-gray-700 text-xs font-semibold px-3 py-2 rounded-lg cursor-pointer shadow-xs transition">
                      <Upload class="h-3.5 w-3.5 text-gray-500" />
                      {{ avatarPreview ? 'Change Logo' : 'Upload Logo' }}
                      <input type="file" accept="image/*" class="hidden" @change="onAvatarChange" />
                    </label>
                    <button
                      v-if="avatarPreview"
                      type="button"
                      @click="handleRemoveAvatar"
                      class="inline-flex items-center gap-1.5 text-red-600 hover:text-red-700 text-xs font-semibold px-3 py-2 rounded-lg border border-red-200 hover:bg-red-50 transition"
                    >
                      <Trash2 class="h-3.5 w-3.5" />
                      Remove
                    </button>
                  </div>
                  <p class="text-xs text-gray-500">Square ratio recommended (e.g. 256×256px). Displays first letter initial if omitted.</p>
                </div>
              </div>
            </div>
          </div>

          <!-- 2. BASIC INFORMATION -->
          <div class="bg-white rounded-2xl shadow-sm border border-gray-200 p-6 space-y-6">
            <div>
              <h2 class="text-lg font-semibold text-gray-900">Company Details</h2>
              <p class="text-sm text-gray-500 mt-0.5">Primary information displayed on your company profile and job postings.</p>
            </div>

            <div class="grid grid-cols-1 sm:grid-cols-2 gap-6">
              <div class="sm:col-span-2">
                <label class="block text-sm font-medium text-gray-700">Company Name *</label>
                <input
                  v-model="name"
                  type="text"
                  required
                  class="mt-1 block w-full rounded-lg border border-gray-300 px-3 py-2 text-sm shadow-xs focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500"
                  placeholder="Acme Corporation"
                />
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700">Legal / Registered Name</label>
                <input
                  v-model="legalName"
                  type="text"
                  class="mt-1 block w-full rounded-lg border border-gray-300 px-3 py-2 text-sm shadow-xs focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500"
                  placeholder="Acme Technologies Inc."
                />
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700">Tagline</label>
                <input
                  v-model="tagline"
                  type="text"
                  class="mt-1 block w-full rounded-lg border border-gray-300 px-3 py-2 text-sm shadow-xs focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500"
                  placeholder="Building the future of software"
                />
              </div>

              <div class="sm:col-span-2">
                <label class="block text-sm font-medium text-gray-700">About / Description</label>
                <textarea
                  v-model="description"
                  rows="4"
                  class="mt-1 block w-full rounded-lg border border-gray-300 px-3 py-2 text-sm shadow-xs focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500"
                  placeholder="Tell candidates about your company's mission, culture, and achievements..."
                ></textarea>
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700">Industry</label>
                <input
                  v-model="industry"
                  type="text"
                  class="mt-1 block w-full rounded-lg border border-gray-300 px-3 py-2 text-sm shadow-xs focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500"
                  placeholder="e.g. Information Technology, Healthcare"
                />
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700">Company Size</label>
                <select
                  v-model="companySize"
                  class="mt-1 block w-full rounded-lg border border-gray-300 px-3 py-2 text-sm shadow-xs focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 bg-white"
                >
                  <option value="">Select size</option>
                  <option value="1-10">1-10 employees</option>
                  <option value="11-50">11-50 employees</option>
                  <option value="51-200">51-200 employees</option>
                  <option value="201-500">201-500 employees</option>
                  <option value="501-1000">501-1000 employees</option>
                  <option value="1000+">1000+ employees</option>
                </select>
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700">Founded Year</label>
                <input
                  v-model.number="foundedYear"
                  type="number"
                  min="1800"
                  max="2099"
                  class="mt-1 block w-full rounded-lg border border-gray-300 px-3 py-2 text-sm shadow-xs focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500"
                  placeholder="e.g. 2018"
                />
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700">Company Type</label>
                <select
                  v-model="companyType"
                  class="mt-1 block w-full rounded-lg border border-gray-300 px-3 py-2 text-sm shadow-xs focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 bg-white"
                >
                  <option value="">Select company type</option>
                  <option value="Privately Held">Privately Held</option>
                  <option value="Public Company">Public Company</option>
                  <option value="Startup">Startup</option>
                  <option value="Non-profit">Non-profit</option>
                  <option value="Government Agency">Government Agency</option>
                  <option value="Partnership">Partnership</option>
                </select>
              </div>
            </div>
          </div>

          <!-- 3. CONTACT & EMAILS -->
          <div class="bg-white rounded-2xl shadow-sm border border-gray-200 p-6 space-y-6">
            <div>
              <h2 class="text-lg font-semibold text-gray-900">Contact & Communication</h2>
              <p class="text-sm text-gray-500 mt-0.5">Official company channels and candidate inquiry contacts.</p>
            </div>

            <div class="grid grid-cols-1 sm:grid-cols-2 gap-6">
              <div>
                <label class="block text-sm font-medium text-gray-700">Official Company Email *</label>
                <div class="relative mt-1">
                  <input
                    v-model="officialEmail"
                    type="email"
                    required
                    class="block w-full rounded-lg border border-gray-300 pl-10 pr-3 py-2 text-sm shadow-xs focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500"
                    placeholder="admin@company.com"
                  />
                  <Mail class="h-4 w-4 text-gray-400 absolute left-3.5 top-3" />
                </div>
                <p class="text-xs text-amber-600 mt-1">
                  Note: Changing your official company email will require re-verifying the email address.
                </p>
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700">Support / Careers Email</label>
                <div class="relative mt-1">
                  <input
                    v-model="supportEmail"
                    type="email"
                    class="block w-full rounded-lg border border-gray-300 pl-10 pr-3 py-2 text-sm shadow-xs focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500"
                    placeholder="careers@company.com"
                  />
                  <Mail class="h-4 w-4 text-gray-400 absolute left-3.5 top-3" />
                </div>
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700">Phone Number</label>
                <div class="relative mt-1">
                  <input
                    v-model="phone"
                    type="tel"
                    class="block w-full rounded-lg border border-gray-300 pl-10 pr-3 py-2 text-sm shadow-xs focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500"
                    placeholder="+1 (555) 012-3456"
                  />
                  <Phone class="h-4 w-4 text-gray-400 absolute left-3.5 top-3" />
                </div>
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700">Website URL</label>
                <div class="relative mt-1">
                  <input
                    v-model="websiteUrl"
                    type="url"
                    class="block w-full rounded-lg border border-gray-300 pl-10 pr-3 py-2 text-sm shadow-xs focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500"
                    placeholder="https://example.com"
                  />
                  <Globe class="h-4 w-4 text-gray-400 absolute left-3.5 top-3" />
                </div>
              </div>
            </div>
          </div>

          <!-- 4. LOCATION & OFFICE -->
          <div class="bg-white rounded-2xl shadow-sm border border-gray-200 p-6 space-y-6">
            <div>
              <h2 class="text-lg font-semibold text-gray-900">Location & Office</h2>
              <p class="text-sm text-gray-500 mt-0.5">Physical headquarters or office location details.</p>
            </div>

            <div class="flex items-center gap-3 p-4 bg-gray-50 rounded-xl border border-gray-200">
              <input
                id="hasOfficeCheck"
                v-model="hasPhysicalOffice"
                type="checkbox"
                class="h-4 w-4 rounded border-gray-300 text-indigo-600 focus:ring-indigo-500"
              />
              <label for="hasOfficeCheck" class="text-sm font-medium text-gray-800 cursor-pointer">
                This organization operates with a physical office location
              </label>
            </div>

            <div v-if="hasPhysicalOffice" class="grid grid-cols-1 sm:grid-cols-2 gap-6 pt-2">
              <div>
                <label class="block text-sm font-medium text-gray-700">Country Code (2-letter ISO)</label>
                <input
                  v-model="countryCode"
                  type="text"
                  maxlength="2"
                  class="mt-1 block w-full rounded-lg border border-gray-300 px-3 py-2 text-sm uppercase shadow-xs focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500"
                  placeholder="US / IN / GB"
                />
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700">State / Province</label>
                <input
                  v-model="stateProvince"
                  type="text"
                  class="mt-1 block w-full rounded-lg border border-gray-300 px-3 py-2 text-sm shadow-xs focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500"
                  placeholder="e.g. California"
                />
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700">City</label>
                <input
                  v-model="city"
                  type="text"
                  class="mt-1 block w-full rounded-lg border border-gray-300 px-3 py-2 text-sm shadow-xs focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500"
                  placeholder="e.g. San Francisco"
                />
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700">Postal Code</label>
                <input
                  v-model="postalCode"
                  type="text"
                  class="mt-1 block w-full rounded-lg border border-gray-300 px-3 py-2 text-sm shadow-xs focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500"
                  placeholder="e.g. 94107"
                />
              </div>

              <div class="sm:col-span-2">
                <label class="block text-sm font-medium text-gray-700">Address Line 1</label>
                <input
                  v-model="addressLine1"
                  type="text"
                  class="mt-1 block w-full rounded-lg border border-gray-300 px-3 py-2 text-sm shadow-xs focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500"
                  placeholder="e.g. 100 Market St, Suite 400"
                />
              </div>

              <div class="sm:col-span-2">
                <label class="block text-sm font-medium text-gray-700">Address Line 2 (Optional)</label>
                <input
                  v-model="addressLine2"
                  type="text"
                  class="mt-1 block w-full rounded-lg border border-gray-300 px-3 py-2 text-sm shadow-xs focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500"
                  placeholder="Building B, Floor 4"
                />
              </div>
            </div>
          </div>

          <!-- 5. SOCIAL & WEB PRESENCE -->
          <div class="bg-white rounded-2xl shadow-sm border border-gray-200 p-6 space-y-6">
            <div>
              <h2 class="text-lg font-semibold text-gray-900">Social & Web Links</h2>
              <p class="text-sm text-gray-500 mt-0.5">Connect your social profiles to increase company visibility.</p>
            </div>

            <div class="grid grid-cols-1 sm:grid-cols-2 gap-6">
              <div>
                <label class="block text-sm font-medium text-gray-700">LinkedIn Profile</label>
                <input
                  v-model="linkedinUrl"
                  type="url"
                  class="mt-1 block w-full rounded-lg border border-gray-300 px-3 py-2 text-sm shadow-xs focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500"
                  placeholder="https://linkedin.com/company/acme"
                />
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700">Twitter / X Profile</label>
                <input
                  v-model="twitterUrl"
                  type="url"
                  class="mt-1 block w-full rounded-lg border border-gray-300 px-3 py-2 text-sm shadow-xs focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500"
                  placeholder="https://x.com/acme"
                />
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700">Facebook Page</label>
                <input
                  v-model="facebookUrl"
                  type="url"
                  class="mt-1 block w-full rounded-lg border border-gray-300 px-3 py-2 text-sm shadow-xs focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500"
                  placeholder="https://facebook.com/acme"
                />
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700">GitHub Organization</label>
                <input
                  v-model="githubUrl"
                  type="url"
                  class="mt-1 block w-full rounded-lg border border-gray-300 px-3 py-2 text-sm shadow-xs focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500"
                  placeholder="https://github.com/acme"
                />
              </div>

              <div class="sm:col-span-2">
                <label class="block text-sm font-medium text-gray-700">Glassdoor Company Profile</label>
                <input
                  v-model="glassdoorUrl"
                  type="url"
                  class="mt-1 block w-full rounded-lg border border-gray-300 px-3 py-2 text-sm shadow-xs focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500"
                  placeholder="https://glassdoor.com/Overview/Working-at-Acme..."
                />
              </div>
            </div>
          </div>

          <!-- Bottom Actions -->
          <div class="flex items-center justify-end gap-3 pt-4">
            <router-link
              :to="`/organizations/${orgId}`"
              class="px-5 py-2.5 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 transition"
            >
              Cancel
            </router-link>
            <button
              type="submit"
              :disabled="saving"
              class="inline-flex items-center gap-2 px-6 py-2.5 text-sm font-semibold text-white bg-indigo-600 rounded-lg hover:bg-indigo-700 shadow-sm disabled:opacity-50 transition"
            >
              <Loader2 v-if="saving" class="h-4 w-4 animate-spin" />
              <Check v-else class="h-4 w-4" />
              {{ saving ? 'Saving Changes…' : 'Save Changes' }}
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

