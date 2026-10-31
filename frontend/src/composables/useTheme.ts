import { ref, computed } from 'vue'

export type ThemePreference = 'light' | 'dark' | 'system'

const THEME_STORAGE_KEY = 'rp-color-theme'

// Shared state across all instances
const currentTheme = ref<ThemePreference>('system')
const isDark = ref(false)
let isInitialized = false

function getSystemPrefersDark(): boolean {
  if (typeof window === 'undefined') return false
  return window.matchMedia('(prefers-color-scheme: dark)').matches
}

function applyTheme(pref: ThemePreference) {
  if (typeof document === 'undefined') return

  const dark = pref === 'dark' || (pref === 'system' && getSystemPrefersDark())
  isDark.value = dark

  if (dark) {
    document.documentElement.classList.add('dark')
    document.documentElement.style.colorScheme = 'dark'
  } else {
    document.documentElement.classList.remove('dark')
    document.documentElement.style.colorScheme = 'light'
  }
}

export function useTheme() {
  function setTheme(pref: ThemePreference) {
    currentTheme.value = pref
    try {
      localStorage.setItem(THEME_STORAGE_KEY, pref)
    } catch (e) {
      // Storage unavailable (e.g. strict privacy mode)
    }
    applyTheme(pref)
  }

  function toggleTheme() {
    // If currently dark, switch to light. If light, switch to dark.
    setTheme(isDark.value ? 'light' : 'dark')
  }

  function initTheme() {
    if (typeof window === 'undefined' || isInitialized) return
    isInitialized = true

    try {
      const stored = localStorage.getItem(THEME_STORAGE_KEY) as ThemePreference | null
      if (stored && ['light', 'dark', 'system'].includes(stored)) {
        currentTheme.value = stored
      } else {
        currentTheme.value = 'system'
      }
    } catch {
      currentTheme.value = 'system'
    }

    applyTheme(currentTheme.value)

    // Listen for OS color scheme changes (when set to 'system')
    const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)')
    mediaQuery.addEventListener('change', () => {
      if (currentTheme.value === 'system') {
        applyTheme('system')
      }
    })
  }

  return {
    theme: computed(() => currentTheme.value),
    isDark: computed(() => isDark.value),
    setTheme,
    toggleTheme,
    initTheme,
  }
}

