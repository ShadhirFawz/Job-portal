<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useRoute, RouterView } from 'vue-router'
import AppNavbar from '@/components/layout/AppNavbar.vue'
import { useTheme } from '@/composables/useTheme'

const route = useRoute()
const { initTheme } = useTheme()

onMounted(() => {
  initTheme()
})

const hideNavbar = computed(() => {
  if (route.meta?.hideNavbar) return true
  const standalonePages = ['login', 'register', 'auth-callback']
  return standalonePages.includes(route.name as string)
})
</script>

<template>
  <div class="min-h-screen bg-[var(--bg-page)] text-[var(--text-main)] transition-colors duration-200 flex flex-col antialiased">
    <AppNavbar v-if="!hideNavbar" />
    <main class="flex-1 flex flex-col">
      <RouterView />
    </main>
    <Toaster position="top-right" richColors />
  </div>
</template>

<style scoped></style>
