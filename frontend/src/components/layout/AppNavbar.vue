<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useOrganizationsStore } from '@/stores/organizations'
import { useTheme, type ThemePreference } from '@/composables/useTheme'
import { getProfileImageUrl } from '@/composables/useProfileImage'
import {
  Building2,
  ChevronDown,
  LogOut,
  Mail,
  Moon,
  PlusCircle,
  Sun,
  Laptop,
  User,
  Menu,
  X,
  Home,
  FileText,
} from 'lucide-vue-next'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const orgStore = useOrganizationsStore()
const { theme, isDark, setTheme, toggleTheme } = useTheme()

const isUserMenuOpen = ref(false)
const isMobileMenuOpen = ref(false)
const userMenuRef = ref<HTMLElement | null>(null)

const user = computed(() => auth.user)
const userRole = computed(() => auth.role || 'candidate')

const displayName = computed(() => {
  if (!user.value) return 'Guest'
  return (
    user.value.display_name ||
    `${user.value.first_name || ''} ${user.value.last_name || ''}`.trim() ||
    user.value.email
  )
})

const userInitial = computed(() => {
  return (displayName.value || 'U').charAt(0).toUpperCase()
})

const userAvatarUrl = computed(() => {
  return user.value?.avatar_path ? getProfileImageUrl(user.value.avatar_path) : undefined
})

const primaryOrg = computed(() => {
  return orgStore.myOrganizations.length > 0 ? orgStore.myOrganizations[0] : null
})

onMounted(async () => {
  if (auth.user) {
    await orgStore.fetchMyOrganizations()
  }
  document.addEventListener('click', handleClickOutside)
})

onBeforeUnmount(() => {
  document.removeEventListener('click', handleClickOutside)
  document.body.style.overflow = ''
})

// Lock body scroll while mobile drawer is open
watch(isMobileMenuOpen, (open) => {
  document.body.style.overflow = open ? 'hidden' : ''
})

// Close mobile drawer on route change
watch(() => route.path, () => {
  isMobileMenuOpen.value = false
})

function handleClickOutside(event: MouseEvent) {
  if (userMenuRef.value && !userMenuRef.value.contains(event.target as Node)) {
    isUserMenuOpen.value = false
  }
}

function handleThemeChange(pref: ThemePreference) {
  setTheme(pref)
}

async function handleSignOut() {
  isUserMenuOpen.value = false
  isMobileMenuOpen.value = false
  await auth.signOut()
  router.push('/login')
}

function closeMenus() {
  isUserMenuOpen.value = false
  isMobileMenuOpen.value = false
}

function isActive(path: string): boolean {
  if (path === '/') return route.path === '/'
  return route.path.startsWith(path)
}
</script>

<template>
  <header
    class="sticky top-0 z-40 w-full border-b backdrop-blur-md transition-colors duration-200 border-border bg-card/85"
  >
    <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
      <div class="flex items-center justify-between h-16">
        <!-- Brand / Logo -->
        <div class="flex items-center gap-8 min-w-0">
          <router-link
            to="/"
            @click="closeMenus"
            class="flex items-center gap-3 group select-none shrink-0"
          >
            <div
              class="h-9 w-9 rounded-xl bg-gradient-to-br from-indigo-600 to-indigo-800 flex items-center justify-center text-white font-bold text-sm shadow-sm group-hover:scale-105 transition-transform"
            >
              RP
            </div>
            <div class="flex flex-col">
              <span class="text-base font-bold tracking-tight text-text-strong leading-tight">
                Recruitment
              </span>
              <span class="text-[10px] font-semibold text-primary tracking-wider uppercase">
                Career Hub
              </span>
            </div>
          </router-link>

          <!-- Desktop Navigation Links (Role-based) -->
          <nav v-if="auth.isAuthenticated" class="hidden md:flex items-center space-x-1">
            <router-link
              to="/"
              :class="[
                'px-3 py-1.5 text-xs font-semibold rounded-lg transition-colors flex items-center gap-1.5',
                isActive('/') && route.path === '/'
                  ? 'bg-primary-light text-primary'
                  : 'text-text-muted hover:text-text-strong hover:bg-surface-raised',
              ]"
            >
              <Home class="h-4 w-4 shrink-0" />
              Dashboard
            </router-link>

            <template v-if="userRole === 'org_admin' || userRole === 'org_staff'">
              <router-link
                v-if="primaryOrg"
                :to="`/organizations/${primaryOrg.id}`"
                :class="[
                  'px-3 py-1.5 text-xs font-semibold rounded-lg transition-colors flex items-center gap-1.5',
                  isActive(`/organizations/${primaryOrg.id}`) && !route.path.includes('/jobs/create')
                    ? 'bg-primary-light text-primary'
                    : 'text-text-muted hover:text-text-strong hover:bg-surface-raised',
                ]"
              >
                <Building2 class="h-3.5 w-3.5" />
                {{ primaryOrg.name }}
              </router-link>

              <router-link
                v-if="primaryOrg"
                :to="`/organizations/${primaryOrg.id}/jobs/create`"
                :class="[
                  'px-3 py-1.5 text-xs font-semibold rounded-lg transition-colors flex items-center gap-1.5',
                  isActive(`/organizations/${primaryOrg.id}/jobs/create`)
                    ? 'bg-primary-light text-primary'
                    : 'text-text-muted hover:text-text-strong hover:bg-surface-raised',
                ]"
              >
                <PlusCircle class="h-3.5 w-3.5" />
                Post Job
              </router-link>
            </template>

            <template v-else-if="userRole === 'candidate'">
              <router-link
                v-if="primaryOrg"
                :to="`/organizations/${primaryOrg.id}`"
                :class="[
                  'px-3 py-1.5 text-xs font-semibold rounded-lg transition-colors flex items-center gap-1.5',
                  isActive(`/organizations/${primaryOrg.id}`)
                    ? 'bg-primary-light text-primary'
                    : 'text-text-muted hover:text-text-strong hover:bg-surface-raised',
                ]"
              >
                <Building2 class="h-3.5 w-3.5" />
                My Company
              </router-link>

              <router-link
                v-else
                to="/organizations/create"
                :class="[
                  'px-3 py-1.5 text-xs font-semibold rounded-lg transition-colors flex items-center gap-1.5',
                  isActive('/organizations/create')
                    ? 'bg-primary-light text-primary'
                    : 'text-text-muted hover:text-text-strong hover:bg-surface-raised',
                ]"
              >
                <Building2 class="h-3.5 w-3.5" />
                Register Company
              </router-link>
            </template>

            <template v-else-if="userRole === 'super_admin'">
              <router-link
                v-if="primaryOrg"
                :to="`/organizations/${primaryOrg.id}`"
                :class="[
                  'px-3 py-1.5 text-xs font-semibold rounded-lg transition-colors flex items-center gap-1.5',
                  isActive(`/organizations/${primaryOrg.id}`)
                    ? 'bg-primary-light text-primary'
                    : 'text-text-muted hover:text-text-strong hover:bg-surface-raised',
                ]"
              >
                <Building2 class="h-3.5 w-3.5" />
                Organizations
              </router-link>
            </template>

            <router-link
              to="/resumes"
              :class="[
                'px-3 py-1.5 text-xs font-semibold rounded-lg transition-colors flex items-center gap-1.5',
                isActive('/resumes')
                  ? 'bg-primary-light text-primary'
                  : 'text-text-muted hover:text-text-strong hover:bg-surface-raised',
              ]"
            >
              <FileText class="h-3.5 w-3.5" />
              Resumes
            </router-link>

            <router-link
              to="/invitations"
              :class="[
                'px-3 py-1.5 text-xs font-semibold rounded-lg transition-colors flex items-center gap-1.5',
                isActive('/invitations')
                  ? 'bg-primary-light text-primary'
                  : 'text-text-muted hover:text-text-strong hover:bg-surface-raised',
              ]"
            >
              <Mail class="h-3.5 w-3.5" />
              Invitations
            </router-link>
          </nav>
        </div>

        <!-- Right Side -->
        <div class="flex items-center gap-2 sm:gap-3 shrink-0">
          <!-- Theme Toggle -->
          <button
            type="button"
            @click="toggleTheme"
            :title="isDark ? 'Switch to Light theme' : 'Switch to Dark theme'"
            class="p-2 rounded-xl text-text-muted hover:text-text-strong hover:bg-surface-raised transition-colors"
            aria-label="Toggle theme"
          >
            <Sun v-if="isDark" class="h-4 w-4 text-amber-400" />
            <Moon v-else class="h-4 w-4 text-primary" />
          </button>

          <!-- Authenticated Account Dropdown (hidden on mobile) -->
          <div v-if="auth.isAuthenticated" ref="userMenuRef" class="relative hidden md:block">
            <button
              type="button"
              @click="isUserMenuOpen = !isUserMenuOpen"
              class="flex items-center gap-2 p-1.5 rounded-xl hover:bg-surface-raised transition-colors select-none group"
              aria-haspopup="true"
              :aria-expanded="isUserMenuOpen"
            >
              <div
                class="h-8 w-8 rounded-full overflow-hidden bg-primary text-white font-bold text-xs flex items-center justify-center ring-2 ring-primary/30 group-hover:ring-primary/60 transition shadow-sm"
              >
                <img
                  v-if="userAvatarUrl"
                  :src="userAvatarUrl"
                  :alt="displayName"
                  class="h-full w-full object-cover"
                />
                <span v-else>{{ userInitial }}</span>
              </div>

              <span class="hidden sm:inline-block text-xs font-semibold text-text-strong max-w-[7.5rem] truncate">
                {{ displayName }}
              </span>
              <ChevronDown
                class="h-3.5 w-3.5 text-text-subtle group-hover:text-text-muted transition-transform duration-200"
                :class="{ 'rotate-180': isUserMenuOpen }"
              />
            </button>

            <transition
              enter-active-class="transition duration-150 ease-out"
              enter-from-class="transform scale-95 opacity-0"
              enter-to-class="transform scale-100 opacity-100"
              leave-active-class="transition duration-100 ease-in"
              leave-from-class="transform scale-100 opacity-100"
              leave-to-class="transform scale-95 opacity-0"
            >
              <div
                v-if="isUserMenuOpen"
                class="absolute right-0 mt-2 w-72 rounded-2xl bg-card border border-border shadow-xl overflow-hidden py-2 z-50 divide-y divide-border"
              >
                <div class="px-4 py-3 bg-surface-raised">
                  <div class="flex items-center gap-3">
                    <div
                      class="h-10 w-10 rounded-full overflow-hidden bg-primary text-white font-bold text-sm flex items-center justify-center shrink-0 shadow-sm"
                    >
                      <img
                        v-if="userAvatarUrl"
                        :src="userAvatarUrl"
                        :alt="displayName"
                        class="h-full w-full object-cover"
                      />
                      <span v-else>{{ userInitial }}</span>
                    </div>

                    <div class="min-w-0 flex-1">
                      <p class="text-xs font-bold text-text-strong truncate">
                        {{ displayName }}
                      </p>
                      <p class="text-[11px] text-text-muted truncate">
                        {{ user?.email }}
                      </p>
                      <div class="mt-1">
                        <span
                          class="inline-flex items-center px-2 py-0.5 rounded-full text-[10px] font-bold uppercase tracking-wider"
                          :class="{
                            'bg-primary-light text-primary border border-primary/30':
                              userRole === 'super_admin' || userRole === 'org_admin',
                            'bg-surface-raised text-text-muted border border-border':
                              userRole === 'org_staff' || userRole === 'candidate',
                          }"
                        >
                          {{ userRole.replace('_', ' ') }}
                        </span>
                      </div>
                    </div>
                  </div>
                </div>

                <div class="py-1 text-xs">
                  <router-link
                    to="/profile/complete"
                    @click="isUserMenuOpen = false"
                    class="flex items-center gap-2.5 px-4 py-2 text-text-muted hover:bg-surface-raised hover:text-primary transition"
                  >
                    <User class="h-4 w-4 text-text-subtle" />
                    <span>Profile & Preferences</span>
                  </router-link>

                  <router-link
                    v-if="primaryOrg"
                    :to="`/organizations/${primaryOrg.id}`"
                    @click="isUserMenuOpen = false"
                    class="flex items-center gap-2.5 px-4 py-2 text-text-muted hover:bg-surface-raised hover:text-primary transition"
                  >
                    <Building2 class="h-4 w-4 text-text-subtle" />
                    <span>Manage {{ primaryOrg.name }}</span>
                  </router-link>

                  <router-link
                    v-if="primaryOrg && (userRole === 'org_admin' || userRole === 'org_staff')"
                    :to="`/organizations/${primaryOrg.id}/jobs/create`"
                    @click="isUserMenuOpen = false"
                    class="flex items-center gap-2.5 px-4 py-2 text-text-muted hover:bg-surface-raised hover:text-primary transition"
                  >
                    <PlusCircle class="h-4 w-4 text-text-subtle" />
                    <span>Post a New Job</span>
                  </router-link>

                  <router-link
                    v-if="!primaryOrg"
                    to="/organizations/create"
                    @click="isUserMenuOpen = false"
                    class="flex items-center gap-2.5 px-4 py-2 text-text-muted hover:bg-surface-raised hover:text-primary transition"
                  >
                    <Building2 class="h-4 w-4 text-text-subtle" />
                    <span>Register an Organization</span>
                  </router-link>

                  <router-link
                    to="/resumes"
                    @click="isUserMenuOpen = false"
                    class="flex items-center gap-2.5 px-4 py-2 text-text-muted hover:bg-surface-raised hover:text-primary transition"
                  >
                    <FileText class="h-4 w-4 text-text-subtle" />
                    <span>My Resumes</span>
                  </router-link>

                  <router-link
                    to="/invitations"
                    @click="isUserMenuOpen = false"
                    class="flex items-center gap-2.5 px-4 py-2 text-text-muted hover:bg-surface-raised hover:text-primary transition"
                  >
                    <Mail class="h-4 w-4 text-text-subtle" />
                    <span>Invitations</span>
                  </router-link>
                </div>

                <div class="px-4 py-2.5 text-xs space-y-1.5">
                  <span class="block text-[11px] font-bold uppercase tracking-wider text-text-subtle">
                    Theme Mode
                  </span>
                  <div class="grid grid-cols-3 gap-1 bg-surface-raised p-0.5 rounded-xl">
                    <button
                      type="button"
                      @click="handleThemeChange('light')"
                      :class="[
                        'flex items-center justify-center gap-1 py-1 text-[11px] font-semibold rounded-lg transition',
                        theme === 'light'
                          ? 'bg-card text-primary shadow-sm'
                          : 'text-text-muted hover:text-text-strong',
                      ]"
                    >
                      <Sun class="h-3 w-3" />
                      <span>Light</span>
                    </button>

                    <button
                      type="button"
                      @click="handleThemeChange('dark')"
                      :class="[
                        'flex items-center justify-center gap-1 py-1 text-[11px] font-semibold rounded-lg transition',
                        theme === 'dark'
                          ? 'bg-card text-primary shadow-sm'
                          : 'text-text-muted hover:text-text-strong',
                      ]"
                    >
                      <Moon class="h-3 w-3" />
                      <span>Dark</span>
                    </button>

                    <button
                      type="button"
                      @click="handleThemeChange('system')"
                      :class="[
                        'flex items-center justify-center gap-1 py-1 text-[11px] font-semibold rounded-lg transition',
                        theme === 'system'
                          ? 'bg-card text-primary shadow-sm'
                          : 'text-text-muted hover:text-text-strong',
                      ]"
                    >
                      <Laptop class="h-3 w-3" />
                      <span>System</span>
                    </button>
                  </div>
                </div>

                <div class="py-1">
                  <button
                    type="button"
                    @click="handleSignOut"
                    class="w-full flex items-center gap-2.5 px-4 py-2 text-xs font-semibold text-danger hover:bg-danger-bg transition"
                  >
                    <LogOut class="h-4 w-4" />
                    <span>Sign Out</span>
                  </button>
                </div>
              </div>
            </transition>
          </div>

          <!-- Unauthenticated Links -->
          <div v-else class="flex items-center gap-2">
            <router-link
              to="/login"
              class="text-xs font-semibold px-3 py-1.5 rounded-lg text-text-muted hover:bg-surface-raised hover:text-text-strong transition"
            >
              Sign In
            </router-link>
            <router-link
              to="/register"
              class="text-xs font-semibold px-3.5 py-1.5 rounded-lg bg-primary hover:bg-primary-hover text-white shadow-sm transition"
            >
              Get Started
            </router-link>
          </div>

          <!-- Mobile Hamburger -->
          <button
            v-if="auth.isAuthenticated"
            type="button"
            @click="isMobileMenuOpen = true"
            class="md:hidden p-2 rounded-xl text-text-muted hover:bg-surface-raised transition"
            aria-label="Open menu"
          >
            <Menu class="h-5 w-5" />
          </button>
        </div>
      </div>
    </div>

    <!-- ==================== MOBILE RIGHT-SIDE DRAWER ==================== -->
    <Teleport to="body">
      <!-- Backdrop -->
      <transition
        enter-active-class="transition-opacity duration-300 ease-out"
        enter-from-class="opacity-0"
        enter-to-class="opacity-100"
        leave-active-class="transition-opacity duration-200 ease-in"
        leave-from-class="opacity-100"
        leave-to-class="opacity-0"
      >
        <div
          v-if="isMobileMenuOpen"
          class="fixed inset-0 bg-black/50 backdrop-blur-sm z-50 md:hidden"
          @click="isMobileMenuOpen = false"
        />
      </transition>

      <!-- Drawer Panel -->
      <transition
        enter-active-class="transition-transform duration-300 ease-out"
        enter-from-class="translate-x-full"
        enter-to-class="translate-x-0"
        leave-active-class="transition-transform duration-250 ease-in"
        leave-from-class="translate-x-0"
        leave-to-class="translate-x-full"
      >
        <aside
          v-if="isMobileMenuOpen"
          class="fixed top-0 right-0 bottom-0 z-50 w-[85vw] max-w-sm md:hidden bg-card border-l border-border shadow-2xl rounded-bl-3xl flex flex-col"
          role="dialog"
          aria-modal="true"
          aria-label="Mobile navigation"
        >
          <!-- Drawer Header -->
          <div class="flex items-center justify-between px-4 py-3 border-b border-border">
            <div class="flex items-center gap-2.5 min-w-0">
              <div
                class="h-8 w-8 rounded-full overflow-hidden bg-primary text-white font-bold text-xs flex items-center justify-center shrink-0"
              >
                <img
                  v-if="userAvatarUrl"
                  :src="userAvatarUrl"
                  :alt="displayName"
                  class="h-full w-full object-cover"
                />
                <span v-else>{{ userInitial }}</span>
              </div>
              <div class="min-w-0">
                <p class="text-xs font-bold text-text-strong truncate">{{ displayName }}</p>
                <p class="text-[10px] text-text-muted truncate">{{ user?.email }}</p>
              </div>
            </div>
            <button
              type="button"
              @click="isMobileMenuOpen = false"
              class="p-2 rounded-xl text-text-muted hover:bg-surface-raised transition shrink-0"
              aria-label="Close menu"
            >
              <X class="h-5 w-5" />
            </button>
          </div>

          <!-- Drawer Body -->
          <div class="flex-1 overflow-y-auto px-3 py-3 space-y-1">
            <!-- Nav Links -->
            <router-link
              to="/"
              @click="closeMenus"
              :class="[
                'flex items-center gap-2.5 px-3 py-2.5 rounded-xl text-sm font-semibold transition',
                isActive('/') && route.path === '/'
                  ? 'bg-primary-light text-primary'
                  : 'text-text-muted hover:bg-surface-raised hover:text-text-strong',
              ]"
            >
              <Home class="h-4 w-4 shrink-0" />
              Dashboard
            </router-link>

            <template v-if="userRole === 'org_admin' || userRole === 'org_staff'">
              <router-link
                v-if="primaryOrg"
                :to="`/organizations/${primaryOrg.id}`"
                @click="closeMenus"
                :class="[
                  'flex items-center gap-2.5 px-3 py-2.5 rounded-xl text-sm font-semibold transition',
                  isActive(`/organizations/${primaryOrg.id}`) && !route.path.includes('/jobs/create')
                    ? 'bg-primary-light text-primary'
                    : 'text-text-muted hover:bg-surface-raised hover:text-text-strong',
                ]"
              >
                <Building2 class="h-4 w-4 shrink-0" />
                <span class="truncate">{{ primaryOrg.name }}</span>
              </router-link>

              <router-link
                v-if="primaryOrg"
                :to="`/organizations/${primaryOrg.id}/jobs/create`"
                @click="closeMenus"
                :class="[
                  'flex items-center gap-2.5 px-3 py-2.5 rounded-xl text-sm font-semibold transition',
                  isActive(`/organizations/${primaryOrg.id}/jobs/create`)
                    ? 'bg-primary-light text-primary'
                    : 'text-text-muted hover:bg-surface-raised hover:text-text-strong',
                ]"
              >
                <PlusCircle class="h-4 w-4 shrink-0" />
                Post a Job
              </router-link>
            </template>

            <template v-else-if="userRole === 'candidate'">
              <router-link
                v-if="primaryOrg"
                :to="`/organizations/${primaryOrg.id}`"
                @click="closeMenus"
                :class="[
                  'flex items-center gap-2.5 px-3 py-2.5 rounded-xl text-sm font-semibold transition',
                  isActive(`/organizations/${primaryOrg.id}`)
                    ? 'bg-primary-light text-primary'
                    : 'text-text-muted hover:bg-surface-raised hover:text-text-strong',
                ]"
              >
                <Building2 class="h-4 w-4 shrink-0" />
                My Company
              </router-link>

              <router-link
                v-else
                to="/organizations/create"
                @click="closeMenus"
                :class="[
                  'flex items-center gap-2.5 px-3 py-2.5 rounded-xl text-sm font-semibold transition',
                  isActive('/organizations/create')
                    ? 'bg-primary-light text-primary'
                    : 'text-text-muted hover:bg-surface-raised hover:text-text-strong',
                ]"
              >
                <Building2 class="h-4 w-4 shrink-0" />
                Register Company
              </router-link>
            </template>

            <router-link
              to="/resumes"
              @click="closeMenus"
              :class="[
                'flex items-center gap-2.5 px-3 py-2.5 rounded-xl text-sm font-semibold transition',
                isActive('/resumes')
                  ? 'bg-primary-light text-primary'
                  : 'text-text-muted hover:bg-surface-raised hover:text-text-strong',
              ]"
            >
              <FileText class="h-4 w-4 shrink-0" />
              My Resumes
            </router-link>

            <router-link
              to="/invitations"
              @click="closeMenus"
              :class="[
                'flex items-center gap-2.5 px-3 py-2.5 rounded-xl text-sm font-semibold transition',
                isActive('/invitations')
                  ? 'bg-primary-light text-primary'
                  : 'text-text-muted hover:bg-surface-raised hover:text-text-strong',
              ]"
            >
              <Mail class="h-4 w-4 shrink-0" />
              Invitations
            </router-link>

            <router-link
              to="/profile/complete"
              @click="closeMenus"
              :class="[
                'flex items-center gap-2.5 px-3 py-2.5 rounded-xl text-sm font-semibold transition',
                isActive('/profile/complete')
                  ? 'bg-primary-light text-primary'
                  : 'text-text-muted hover:bg-surface-raised hover:text-text-strong',
              ]"
            >
              <User class="h-4 w-4 shrink-0" />
              Profile & Settings
            </router-link>

            <!-- Theme Mode (mobile) -->
            <div class="pt-3 mt-2 border-t border-border">
              <span class="block px-3 mb-1.5 text-[11px] font-bold uppercase tracking-wider text-text-subtle">
                Theme Mode
              </span>
              <div class="grid grid-cols-3 gap-1 bg-surface-raised p-1 rounded-xl mx-1">
                <button
                  type="button"
                  @click="handleThemeChange('light')"
                  :class="[
                    'flex items-center justify-center gap-1 py-1.5 text-xs font-semibold rounded-lg transition',
                    theme === 'light'
                      ? 'bg-card text-primary shadow-sm'
                      : 'text-text-muted hover:text-text-strong',
                  ]"
                >
                  <Sun class="h-3.5 w-3.5" />
                  Light
                </button>
                <button
                  type="button"
                  @click="handleThemeChange('dark')"
                  :class="[
                    'flex items-center justify-center gap-1 py-1.5 text-xs font-semibold rounded-lg transition',
                    theme === 'dark'
                      ? 'bg-card text-primary shadow-sm'
                      : 'text-text-muted hover:text-text-strong',
                  ]"
                >
                  <Moon class="h-3.5 w-3.5" />
                  Dark
                </button>
                <button
                  type="button"
                  @click="handleThemeChange('system')"
                  :class="[
                    'flex items-center justify-center gap-1 py-1.5 text-xs font-semibold rounded-lg transition',
                    theme === 'system'
                      ? 'bg-card text-primary shadow-sm'
                      : 'text-text-muted hover:text-text-strong',
                  ]"
                >
                  <Laptop class="h-3.5 w-3.5" />
                  System
                </button>
              </div>
            </div>
          </div>

          <!-- Drawer Footer -->
          <div class="border-t border-border p-3">
            <button
              type="button"
              @click="handleSignOut"
              class="w-full flex items-center justify-center gap-2 px-3 py-2.5 rounded-xl text-sm font-semibold text-danger hover:bg-danger-bg transition"
            >
              <LogOut class="h-4 w-4" />
              Sign Out
            </button>
          </div>
        </aside>
      </transition>
    </Teleport>
  </header>
</template>