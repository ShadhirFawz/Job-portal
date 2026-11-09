<script setup lang="ts">
import { onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'

const route = useRoute()
const router = useRouter()
const organizationId = route.params.orgId as string

onMounted(() => {
  router.replace(`/organizations/${organizationId}/questions`)
})
</script>

<template>
  <div class="min-h-screen bg-page flex items-center justify-center p-4">
    <div class="h-6 w-6 animate-spin rounded-full border-2 border-primary border-t-transparent"></div>
  </div>
</template>
