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
import { useAuthStore } from '@/stores/auth'
import { useOrganizationsStore } from '@/stores/organizations'
import { organizationService } from '@/services/organization.service'
import type { OrganizationUpdateInput } from '@/types/organization'
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
    const org = await organizationService.getOrganization(orgId)

    if (!org) {
      toast.error('Organization not found.')
      router.replace('/organizations')
      return
    }

    // 2. Check authorization: must be creator, super_admin, or active org_admin member
    const isAllowed = await organizationService.checkIsOrgAdmin(
      orgId,
      auth.user.id,
      auth.user.role,
      org.created_by
    )

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

    await organizationService.updateOrganization(orgId, updatePayload)

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
  <div class="min-h-screen bg-page py-6 sm:py-10 px-3 sm:px-6 lg:px-8">
    <div class="max-w-4xl mx-auto space-y-6">
      
      <!-- Loading State -->
      <div v-if="pageLoading" class="flex flex-col items-center justify-center py-24 text-text-muted">
        <Loader2 class="h-8 w-8 animate-spin text-primary" />
        <p class="mt-3 text-xs sm:text-sm">Verifying permissions and loading company details…</p>
      </div>

      <!-- Unauthorized State -->
      <div
        v-else-if="!authorized"
        class="bg-card rounded-xl sm:rounded-2xl shadow-sm border border-danger-border p-6 sm:p-8 text-center space-y-4"
      >
        <div class="h-12 w-12 rounded-full bg-danger-bg text-danger flex items-center justify-center mx-auto">
          <ShieldAlert class="h-6 w-6" />
        </div>
        <h2 class="text-lg sm:text-xl font-bold text-text-strong">Access Denied</h2>
        <p class="text-xs sm:text-sm text-text-muted max-w-md mx-auto leading-relaxed">
          Only organization administrators can edit this company profile.
        </p>
        <router-link
          :to="`/organizations/${orgId}`"
          class="inline-flex items-center gap-2 px-4 py-2 bg-primary text-white rounded-lg text-xs sm:text-sm font-semibold hover:bg-primary-hover transition shadow-sm"
        >
          <ArrowLeft class="h-4 w-4" /> Return to Organization
        </router-link>
      </div>

      <!-- Edit Form -->
      <div v-else class="space-y-6">
        
        <!-- Top Navigation & Heading -->
        <div class="flex flex-col lg:flex-row lg:items-center justify-between gap-4">
          <div class="space-y-1">
            <router-link
              :to="`/organizations/${orgId}`"
              class="inline-flex items-center gap-1.5 text-xs sm:text-sm font-medium text-text-muted hover:text-text-strong transition mb-1 focus:outline-none focus:ring-2 focus:ring-primary/25 rounded-md"
            >
              <ArrowLeft class="h-4 w-4" />
              Back to Company Profile
            </router-link>
            <h1 class="text-xl sm:text-2xl lg:text-3xl font-bold text-text-strong tracking-tight">
              Edit Company Profile
            </h1>
            <p class="text-xs sm:text-sm text-text-secondary leading-relaxed">
              Manage company details, visual branding, contact information, and locations.
            </p>
          </div>

          <div class="flex items-center gap-2.5 self-start lg:self-auto">
            <router-link
              :to="`/organizations/${orgId}`"
              class="px-4 py-2.5 text-xs sm:text-sm font-medium text-text-secondary bg-card border border-border rounded-xl hover:bg-surface-raised transition shadow-xs"
            >
              Cancel
            </router-link>
            <button
              type="button"
              :disabled="saving"
              @click="handleSave"
              class="inline-flex items-center gap-2 px-5 py-2.5 text-xs sm:text-sm font-semibold text-white bg-primary hover:bg-primary-hover rounded-xl shadow-sm disabled:opacity-50 transition"
            >
              <Loader2 v-if="saving" class="h-4 w-4 animate-spin" />
              <Check v-else class="h-4 w-4" />
              {{ saving ? 'Saving…' : 'Save Changes' }}
            </button>
          </div>
        </div>

        <!-- Error Message Banner -->
        <div v-if="errorMsg" class="alert alert-error p-4 text-xs sm:text-sm flex items-start gap-3">
          <AlertCircle class="h-5 w-5 shrink-0 text-danger mt-0.5" />
          <span class="leading-relaxed">{{ errorMsg }}</span>
        </div>

        <form @submit.prevent="handleSave" class="space-y-6">
          
          <!-- 1. BRANDING & MEDIA -->
          <div class="bg-card rounded-xl sm:rounded-2xl shadow-sm border border-border overflow-hidden">
            <div class="p-4 sm:p-6 border-b border-border">
              <h2 class="text-base sm:text-lg font-semibold text-text-strong">Visual Branding</h2>
              <p class="text-xs sm:text-sm text-text-muted mt-0.5">Upload high-resolution images for your company cover and logo.</p>
            </div>

            <!-- Cover Image Box -->
            <div class="p-4 sm:p-6 space-y-6">
              <div>
                <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-2">Company Cover Banner</label>
                <div class="relative h-40 sm:h-52 rounded-xl sm:rounded-2xl overflow-hidden bg-gradient-to-r from-brand-from via-brand-via to-brand-to border border-border shadow-inner group">
                  
                  <!-- Watermark when no image preview -->
                  <div
                    v-if="!coverPreview"
                    class="absolute inset-0 flex items-center justify-end pr-10 pointer-events-none select-none opacity-20"
                  >
                    <span class="text-7xl sm:text-9xl font-black text-white uppercase">{{ companyInitial }}</span>
                  </div>

                  <img
                    v-if="coverPreview"
                    :src="coverPreview"
                    class="w-full h-full object-cover"
                    alt="Cover preview"
                  />
                  <div class="absolute inset-0 bg-black/20 pointer-events-none"></div>

                  <!-- Cover Buttons Overlay -->
                  <div class="absolute bottom-3 right-3 flex items-center gap-2">
                    <label class="inline-flex items-center gap-1.5 bg-card/90 hover:bg-card text-text-secondary hover:text-text-strong text-xs font-semibold px-3 py-1.5 rounded-lg cursor-pointer shadow-md backdrop-blur-xs transition border border-border">
                      <Camera class="h-3.5 w-3.5 text-text-muted" />
                      {{ coverPreview ? 'Change Cover' : 'Upload Cover' }}
                      <input type="file" accept="image/*" class="hidden" @change="onCoverChange" />
                    </label>
                    <button
                      v-if="coverPreview"
                      type="button"
                      @click="handleRemoveCover"
                      class="inline-flex items-center gap-1 bg-danger/90 hover:bg-danger text-white text-xs font-semibold px-2.5 py-1.5 rounded-lg shadow-md backdrop-blur-xs transition"
                      title="Remove cover image"
                    >
                      <Trash2 class="h-3.5 w-3.5" />
                    </button>
                  </div>
                </div>
                <p class="text-[11px] sm:text-xs text-text-muted mt-2">Recommended: 1200×400px. JPG, PNG or WebP up to 5MB.</p>
              </div>

              <!-- Avatar / Logo Box -->
              <div class="pt-2 flex flex-col sm:flex-row items-start sm:items-center gap-4 sm:gap-6">
                <div class="relative">
                  <div class="h-20 w-20 sm:h-24 sm:w-24 rounded-xl sm:rounded-2xl ring-4 ring-card bg-primary text-white overflow-hidden shadow-md flex items-center justify-center flex-shrink-0">
                    <img
                      v-if="avatarPreview"
                      :src="avatarPreview"
                      class="h-full w-full object-cover"
                      alt="Avatar preview"
                    />
                    <span v-else class="text-2xl sm:text-3xl font-bold select-none">
                      {{ companyInitial }}
                    </span>
                  </div>
                </div>

                <div class="space-y-2 min-w-0 flex-1">
                  <span class="block text-xs sm:text-sm font-medium text-text-secondary">Company Logo / Avatar</span>
                  <div class="flex flex-wrap items-center gap-2">
                    <label class="inline-flex items-center gap-1.5 bg-card border border-border hover:bg-surface-raised text-text-secondary text-xs font-semibold px-3 py-2 rounded-lg cursor-pointer shadow-xs transition">
                      <Upload class="h-3.5 w-3.5 text-text-muted flex-shrink-0" />
                      {{ avatarPreview ? 'Change Logo' : 'Upload Logo' }}
                      <input type="file" accept="image/*" class="hidden" @change="onAvatarChange" />
                    </label>
                    <button
                      v-if="avatarPreview"
                      type="button"
                      @click="handleRemoveAvatar"
                      class="inline-flex items-center gap-1.5 text-danger hover:text-danger-hover text-xs font-semibold px-3 py-2 rounded-lg border border-danger-border hover:bg-danger-bg transition"
                    >
                      <Trash2 class="h-3.5 w-3.5 flex-shrink-0" />
                      Remove
                    </button>
                  </div>
                  <p class="text-[11px] sm:text-xs text-text-muted">Square ratio recommended (e.g. 256×256px). Displays first letter initial if omitted.</p>
                </div>
              </div>
            </div>
          </div>

          <!-- 2. BASIC INFORMATION -->
          <div class="bg-card rounded-xl sm:rounded-2xl shadow-sm border border-border p-4 sm:p-6 lg:p-8 space-y-6">
            <div>
              <h2 class="text-base sm:text-lg font-semibold text-text-strong">Company Details</h2>
              <p class="text-xs sm:text-sm text-text-muted mt-0.5">Primary information displayed on your company profile and job postings.</p>
            </div>

            <div class="grid grid-cols-1 sm:grid-cols-2 gap-4 sm:gap-6">
              <div class="sm:col-span-2">
                <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">
                  Company Name <span class="text-danger">*</span>
                </label>
                <input
                  v-model="name"
                  type="text"
                  required
                  placeholder="Acme Corporation"
                />
              </div>

              <div>
                <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">Legal / Registered Name</label>
                <input
                  v-model="legalName"
                  type="text"
                  placeholder="Acme Technologies Inc."
                />
              </div>

              <div>
                <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">Tagline</label>
                <input
                  v-model="tagline"
                  type="text"
                  placeholder="Building the future of software"
                />
              </div>

              <div class="sm:col-span-2">
                <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">About / Description</label>
                <textarea
                  v-model="description"
                  rows="4"
                  placeholder="Tell candidates about your company's mission, culture, and achievements..."
                ></textarea>
              </div>

              <div>
                <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">Industry</label>
                <input
                  v-model="industry"
                  type="text"
                  placeholder="e.g. Information Technology, Healthcare"
                />
              </div>

              <div>
                <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">Company Size</label>
                <select v-model="companySize">
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
                <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">Founded Year</label>
                <input
                  v-model.number="foundedYear"
                  type="number"
                  min="1800"
                  max="2099"
                  placeholder="e.g. 2018"
                />
              </div>

              <div>
                <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">Company Type</label>
                <select v-model="companyType">
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
          <div class="bg-card rounded-xl sm:rounded-2xl shadow-sm border border-border p-4 sm:p-6 lg:p-8 space-y-6">
            <div>
              <h2 class="text-base sm:text-lg font-semibold text-text-strong">Contact & Communication</h2>
              <p class="text-xs sm:text-sm text-text-muted mt-0.5">Official company channels and candidate inquiry contacts.</p>
            </div>

            <div class="grid grid-cols-1 sm:grid-cols-2 gap-4 sm:gap-6">
              <div>
                <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">
                  Official Company Email <span class="text-danger">*</span>
                </label>
                <div class="relative">
                  <input
                    v-model="officialEmail"
                    type="email"
                    required
                    class="pl-10"
                    placeholder="admin@company.com"
                  />
                  <Mail class="h-4 w-4 text-text-subtle absolute left-3.5 top-1/2 -translate-y-1/2 pointer-events-none" />
                </div>
                <p class="text-[11px] sm:text-xs text-warning-text mt-1">
                  Note: Changing your official company email will require re-verifying the email address.
                </p>
              </div>

              <div>
                <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">Support / Careers Email</label>
                <div class="relative">
                  <input
                    v-model="supportEmail"
                    type="email"
                    class="pl-10"
                    placeholder="careers@company.com"
                  />
                  <Mail class="h-4 w-4 text-text-subtle absolute left-3.5 top-1/2 -translate-y-1/2 pointer-events-none" />
                </div>
              </div>

              <div>
                <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">Phone Number</label>
                <div class="relative">
                  <input
                    v-model="phone"
                    type="tel"
                    class="pl-10"
                    placeholder="+1 (555) 012-3456"
                  />
                  <Phone class="h-4 w-4 text-text-subtle absolute left-3.5 top-1/2 -translate-y-1/2 pointer-events-none" />
                </div>
              </div>

              <div>
                <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">Website URL</label>
                <div class="relative">
                  <input
                    v-model="websiteUrl"
                    type="url"
                    class="pl-10"
                    placeholder="https://example.com"
                  />
                  <Globe class="h-4 w-4 text-text-subtle absolute left-3.5 top-1/2 -translate-y-1/2 pointer-events-none" />
                </div>
              </div>
            </div>
          </div>

          <!-- 4. LOCATION & OFFICE -->
          <div class="bg-card rounded-xl sm:rounded-2xl shadow-sm border border-border p-4 sm:p-6 lg:p-8 space-y-6">
            <div>
              <h2 class="text-base sm:text-lg font-semibold text-text-strong">Location & Office</h2>
              <p class="text-xs sm:text-sm text-text-muted mt-0.5">Physical headquarters or office location details.</p>
            </div>

            <div class="flex items-center gap-3 p-4 bg-surface-raised rounded-xl border border-border">
              <input
                id="hasOfficeCheck"
                v-model="hasPhysicalOffice"
                type="checkbox"
                class="h-4.5 w-4.5 rounded border-border text-primary focus:ring-2 focus:ring-primary/25 cursor-pointer"
              />
              <label for="hasOfficeCheck" class="text-xs sm:text-sm font-medium text-text-strong cursor-pointer select-none mb-0">
                This organization operates with a physical office location
              </label>
            </div>

            <div v-if="hasPhysicalOffice" class="grid grid-cols-1 sm:grid-cols-2 gap-4 sm:gap-6 pt-2">
              <div>
                <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">Country Code (2-letter ISO)</label>
                <input
                  v-model="countryCode"
                  type="text"
                  maxlength="2"
                  class="uppercase"
                  placeholder="US / IN / GB"
                />
              </div>

              <div>
                <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">State / Province</label>
                <input
                  v-model="stateProvince"
                  type="text"
                  placeholder="e.g. California"
                />
              </div>

              <div>
                <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">City</label>
                <input
                  v-model="city"
                  type="text"
                  placeholder="e.g. San Francisco"
                />
              </div>

              <div>
                <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">Postal Code</label>
                <input
                  v-model="postalCode"
                  type="text"
                  placeholder="e.g. 94107"
                />
              </div>

              <div class="sm:col-span-2">
                <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">Address Line 1</label>
                <input
                  v-model="addressLine1"
                  type="text"
                  placeholder="e.g. 100 Market St, Suite 400"
                />
              </div>

              <div class="sm:col-span-2">
                <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">Address Line 2 (Optional)</label>
                <input
                  v-model="addressLine2"
                  type="text"
                  placeholder="Building B, Floor 4"
                />
              </div>
            </div>
          </div>

          <!-- 5. SOCIAL & WEB PRESENCE -->
          <div class="bg-card rounded-xl sm:rounded-2xl shadow-sm border border-border p-4 sm:p-6 lg:p-8 space-y-6">
            <div>
              <h2 class="text-base sm:text-lg font-semibold text-text-strong">Social & Web Links</h2>
              <p class="text-xs sm:text-sm text-text-muted mt-0.5">Connect your social profiles to increase company visibility.</p>
            </div>

            <div class="grid grid-cols-1 sm:grid-cols-2 gap-4 sm:gap-6">
              <div>
                <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">LinkedIn Profile</label>
                <input
                  v-model="linkedinUrl"
                  type="url"
                  placeholder="https://linkedin.com/company/acme"
                />
              </div>

              <div>
                <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">Twitter / X Profile</label>
                <input
                  v-model="twitterUrl"
                  type="url"
                  placeholder="https://x.com/acme"
                />
              </div>

              <div>
                <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">Facebook Page</label>
                <input
                  v-model="facebookUrl"
                  type="url"
                  placeholder="https://facebook.com/acme"
                />
              </div>

              <div>
                <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">GitHub Organization</label>
                <input
                  v-model="githubUrl"
                  type="url"
                  placeholder="https://github.com/acme"
                />
              </div>

              <div class="sm:col-span-2">
                <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">Glassdoor Company Profile</label>
                <input
                  v-model="glassdoorUrl"
                  type="url"
                  placeholder="https://glassdoor.com/Overview/Working-at-Acme..."
                />
              </div>
            </div>
          </div>

          <!-- Bottom Actions -->
          <div class="flex flex-col-reverse sm:flex-row items-stretch sm:items-center justify-end gap-3 pt-4">
            <router-link
              :to="`/organizations/${orgId}`"
              class="px-5 py-3 text-xs sm:text-sm font-medium text-text-secondary bg-card border border-border rounded-xl hover:bg-surface-raised transition text-center shadow-xs"
            >
              Cancel
            </router-link>
            <button
              type="submit"
              :disabled="saving"
              class="inline-flex items-center justify-center gap-2 px-6 py-3 text-xs sm:text-sm font-semibold text-white bg-primary hover:bg-primary-hover rounded-xl shadow-sm disabled:opacity-50 transition"
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