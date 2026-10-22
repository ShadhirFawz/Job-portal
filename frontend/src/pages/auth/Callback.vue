<script setup lang="ts">
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { supabase } from '@/lib/supabase'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const auth = useAuthStore()

onMounted(async () => {
  // Supabase handles the hash/code automatically
  const { data: { session } } = await supabase.auth.getSession()
  
  if (session) {
    await auth.fetchUserProfile()
    // Redirect based on profile completeness later
    router.replace('/')
  } else {
    router.replace('/login')
  }
})
</script>

<template>
  <div class="flex items-center justify-center min-h-screen">
    <p>Completing authentication...</p>
  </div>
</template>