<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { organizationService } from '@/services/organization.service'
import { profileService } from '@/services/profile.service'
import { uploadOrganizationImage } from '@/composables/useOrganizationImage'
import LocationPickerMap from '@/components/ui/LocationPickerMap.vue'
import { Camera, Building, Globe, Phone, MapPin, Sparkles, Building2, Compass, LocateFixed } from 'lucide-vue-next'

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
const companyType = ref('')
const foundedYear = ref<number | null>(null)

// Location & Address
const hasPhysicalOffice = ref(false)
const countryCode = ref('')
const stateProvince = ref('')
const city = ref('')
const postalCode = ref('')
const addressLine1 = ref('')
const addressLine2 = ref('')
const latitude = ref<number | null>(null)
const longitude = ref<number | null>(null)

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
        company_type: companyType.value.trim() || null,
        founded_year: foundedYear.value,
        has_physical_office: hasPhysicalOffice.value,
        country_code: countryCode.value.trim() || null,
        state_province: stateProvince.value.trim() || null,
        city: city.value.trim() || null,
        postal_code: postalCode.value.trim() || null,
        address_line1: addressLine1.value.trim() || null,
        address_line2: addressLine2.value.trim() || null,
        latitude: latitude.value,
        longitude: longitude.value,
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
  <div class="min-h-screen bg-page py-6 sm:py-10 px-3 sm:px-6 lg:px-8">
    <div class="max-w-3xl mx-auto space-y-6">
      
      <!-- Header section -->
      <div class="mb-4 sm:mb-6">
        <h1 class="text-2xl sm:text-3xl lg:text-4xl font-bold text-text-strong tracking-tight">
          Create a Company
        </h1>
        <p class="mt-1.5 text-xs sm:text-sm lg:text-base text-text-secondary leading-relaxed">
          Set up your organization profile to start posting jobs and managing recruitment.
        </p>
      </div>

      <form @submit.prevent="handleCreate" class="bg-card shadow-sm border border-border rounded-xl sm:rounded-2xl overflow-hidden space-y-6">
        
        <!-- Cover + Avatar Upload Area -->
        <div class="h-36 sm:h-44 bg-gradient-to-r from-brand-from via-brand-via to-brand-to relative">
          <img v-if="coverPreview" :src="coverPreview" class="w-full h-full object-cover" alt="Cover preview" />
          <label class="absolute bottom-3 right-3 bg-card/90 backdrop-blur-xs text-text-secondary hover:text-text-strong hover:bg-card text-xs font-semibold px-3 py-1.5 rounded-lg cursor-pointer shadow-sm border border-border transition">
            Upload Cover
            <input type="file" accept="image/*" class="hidden" @change="onCoverChange" />
          </label>
        </div>

        <div class="px-4 sm:px-8 -mt-10 sm:-mt-12 flex items-end gap-4 relative z-10">
          <div class="relative">
            <div class="h-20 w-20 sm:h-24 sm:w-24 rounded-2xl ring-4 ring-card bg-primary text-white overflow-hidden flex items-center justify-center shadow-md font-bold text-xl sm:text-2xl">
              <img v-if="avatarPreview" :src="avatarPreview" class="h-full w-full object-cover" alt="Avatar preview" />
              <span v-else>{{ (name || 'C').charAt(0).toUpperCase() }}</span>
            </div>
            <label class="absolute bottom-0 right-0 translate-x-1 translate-y-1 bg-primary hover:bg-primary-hover text-white p-2 rounded-full cursor-pointer shadow-md transition">
              <Camera class="h-3.5 w-3.5" />
              <input type="file" accept="image/*" class="hidden" @change="onAvatarChange" />
            </label>
          </div>
        </div>

        <div class="px-4 sm:px-8 pb-8 space-y-6 sm:space-y-8">
          
          <!-- Error alert -->
          <div v-if="errorMsg" class="alert alert-error p-4 text-xs sm:text-sm">
            {{ errorMsg }}
          </div>

          <!-- Basic Info Section -->
          <div class="space-y-4">
            <h2 class="text-base sm:text-lg font-semibold text-text-strong flex items-center gap-2 border-b border-border pb-3">
              Basic Information
            </h2>

            <div class="grid grid-cols-1 sm:grid-cols-2 gap-4 sm:gap-6">
              <div class="sm:col-span-2">
                <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">
                  Company Name <span class="text-danger">*</span>
                </label>
                <input v-model="name" required type="text" placeholder="e.g. Acme Corporation" />
              </div>

              <div>
                <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">Legal Name</label>
                <input v-model="legalName" type="text" placeholder="e.g. Acme Corp Inc." />
              </div>

              <div>
                <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">
                  Official Company Email <span class="text-danger">*</span>
                </label>
                <input v-model="officialEmail" required type="email" placeholder="hr@acme.com" />
                <p class="mt-1 text-[11px] sm:text-xs text-text-muted">Must be a company domain email. Verification required.</p>
              </div>
            </div>

            <div>
              <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">Tagline</label>
              <input v-model="tagline" type="text" placeholder="Short one-liner about your company mission" />
            </div>

            <div>
              <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">Description</label>
              <textarea v-model="description" rows="4" placeholder="Tell candidates about your culture, tech stack, and what makes your company unique..."></textarea>
            </div>
          </div>

          <!-- Extra Details Section -->
          <div class="space-y-4 pt-2">
            <h2 class="text-base sm:text-lg font-semibold text-text-strong flex items-center gap-2 border-b border-border pb-3">
              <Building2 class="h-5 w-5 text-primary" /> Company Details
            </h2>

            <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 sm:gap-6">
              <div>
                <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">Industry</label>
                <input v-model="industry" type="text" placeholder="e.g. Fintech, SaaS" />
              </div>
              <div>
                <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">Company Size</label>
                <select v-model="companySize">
                  <option value="">Select Size</option>
                  <option value="1-10">1-10</option>
                  <option value="11-50">11-50</option>
                  <option value="51-200">51-200</option>
                  <option value="201-500">201-500</option>
                  <option value="501-1000">501-1000</option>
                  <option value="1000+">1000+</option>
                </select>
              </div>
              <div>
                <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">Company Type</label>
                <select v-model="companyType">
                  <option value="">Select Type</option>
                  <option value="Private">Private</option>
                  <option value="Public">Public</option>
                  <option value="Startup">Startup</option>
                  <option value="Non-profit">Non-profit</option>
                  <option value="Government">Government</option>
                </select>
              </div>
              <div>
                <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">Founded Year</label>
                <input v-model.number="foundedYear" type="number" min="1800" max="2026" placeholder="2020" />
              </div>
            </div>
          </div>

          <!-- Contact Section -->
          <div class="space-y-4 pt-2">
            <h2 class="text-base sm:text-lg font-semibold text-text-strong flex items-center gap-2 border-b border-border pb-3">
              <Globe class="h-5 w-5 text-primary" /> Online Presence & Contact
            </h2>

            <div class="grid grid-cols-1 sm:grid-cols-2 gap-4 sm:gap-6">
              <div>
                <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">Website</label>
                <input v-model="websiteUrl" type="url" placeholder="https://example.com" />
              </div>
              <div>
                <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">Phone</label>
                <input v-model="phone" type="tel" placeholder="+1 (555) 000-0000" />
              </div>
            </div>
          </div>

          <!-- Location Section -->
          <div class="space-y-4 pt-2 border-t border-border">
            <h2 class="text-base sm:text-lg font-semibold text-text-strong flex items-center gap-2 border-b border-border pb-3">
              <MapPin class="h-5 w-5 text-primary" /> Physical Location & Address
            </h2>

            <div class="flex items-center gap-2.5 pt-1">
              <input id="hasOffice" v-model="hasPhysicalOffice" type="checkbox" class="h-4.5 w-4.5 rounded border-border" />
              <label for="hasOffice" class="text-xs sm:text-sm font-medium text-text-strong cursor-pointer mb-0">
                This company operates with a physical office location
              </label>
            </div>

            <!-- Interactive Map & Address Form -->
            <div v-if="hasPhysicalOffice" class="space-y-6 pt-2">
              <!-- Location Picker Map -->
              <div class="space-y-1.5">
                <div class="flex items-center justify-between">
                  <label class="block text-xs sm:text-sm font-semibold text-text-strong">
                    Select Exact Location on Real Map
                  </label>
                  <span class="text-[11px] text-text-muted">
                    Click map, drag marker, or search address to auto-populate fields
                  </span>
                </div>

                <LocationPickerMap
                  v-model:latitude="latitude"
                  v-model:longitude="longitude"
                  v-model:address-line1="addressLine1"
                  v-model:address-line2="addressLine2"
                  v-model:city="city"
                  v-model:state-province="stateProvince"
                  v-model:postal-code="postalCode"
                  v-model:country-code="countryCode"
                />
              </div>

              <!-- Manual Address Fields (Auto-filled by map & manually editable) -->
              <div class="bg-surface-raised p-4 sm:p-5 rounded-2xl border border-border space-y-4">
                <div class="flex items-center justify-between border-b border-border pb-2.5">
                  <h3 class="text-xs sm:text-sm font-bold text-text-strong flex items-center gap-1.5">
                    <Compass class="h-4 w-4 text-primary" /> Address Details
                  </h3>
                  <div v-if="latitude && longitude" class="text-[11px] text-primary font-mono flex items-center font-medium gap-1">
                    <LocateFixed class="h-4 w-4" />
                    <span>{{ latitude.toFixed(4) }}, {{ longitude.toFixed(4) }}</span> 
                  </div>
                </div>

                <div class="grid grid-cols-1 sm:grid-cols-2 gap-4 sm:gap-6">
                  <div>
                    <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">
                      Country Code (2-letter ISO)
                    </label>
                    <input
                      v-model="countryCode"
                      type="text"
                      maxlength="2"
                      placeholder="US / LK / GB"
                      class="uppercase font-mono"
                    />
                  </div>

                  <div>
                    <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">
                      State / Province
                    </label>
                    <input
                      v-model="stateProvince"
                      type="text"
                      placeholder="e.g. California / Western Province"
                    />
                  </div>

                  <div>
                    <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">City</label>
                    <input v-model="city" type="text" placeholder="e.g. San Francisco / Colombo" />
                  </div>

                  <div>
                    <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">
                      Postal Code / ZIP
                    </label>
                    <input v-model="postalCode" type="text" placeholder="e.g. 94103 / 00100" />
                  </div>

                  <div class="sm:col-span-2">
                    <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">
                      Address Line 1 (Street & Building No.)
                    </label>
                    <input
                      v-model="addressLine1"
                      type="text"
                      placeholder="e.g. 123 Innovation Drive, Floor 4"
                    />
                  </div>

                  <div class="sm:col-span-2">
                    <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">
                      Address Line 2 (Suite, Unit, Landmark - Optional)
                    </label>
                    <input
                      v-model="addressLine2"
                      type="text"
                      placeholder="e.g. Suite 200, Near Central Station"
                    />
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- Submit Button Action -->
          <div class="pt-4 flex justify-end">
            <button
              type="submit"
              :disabled="loading"
              class="btn btn-primary px-6 sm:px-8 py-2.5 text-xs sm:text-sm font-semibold rounded-xl shadow-sm transition"
            >
              {{ loading ? 'Creating Company...' : 'Create Company' }}
            </button>
          </div>
        </div>
      </form>
    </div>
  </div>
</template>