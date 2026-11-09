<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { questionBankService } from '@/services/questionBank.service'
import type { QuestionBank } from '@/types/assessment'
import {
  Layers,
  Plus,
  ArrowLeft,
  ChevronRight,
  HelpCircle,
  FileQuestion,
  Sparkles
} from 'lucide-vue-next'

const route = useRoute()
const router = useRouter()
const organizationId = route.params.orgId as string

const banks = ref<QuestionBank[]>([])
const loading = ref(true)

onMounted(async () => {
  loading.value = true
  try {
    banks.value = await questionBankService.getOrganizationBanks(organizationId)
  } catch (err) {
    console.error('Failed to load banks:', err)
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <div class="min-h-screen bg-page py-6 sm:py-8 px-4 sm:px-6 lg:px-8">
    <div class="max-w-5xl mx-auto space-y-6">
      
      <!-- Header -->
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-border pb-5">
        <div>
          <button
            type="button"
            @click="router.push(`/organizations/${organizationId}?tab=jobs`)"
            class="inline-flex items-center gap-1.5 text-xs sm:text-sm font-medium text-text-muted hover:text-text-strong transition mb-2"
          >
            <ArrowLeft class="h-4 w-4" />
            <span>Back to Organization</span>
          </button>
          <h1 class="text-2xl sm:text-3xl font-bold tracking-tight text-text-strong">
            Assessments & Question Banks
          </h1>
          <p class="text-xs sm:text-sm text-text-muted mt-1">
            Manage question banks and assessment stages for candidate evaluations.
          </p>
        </div>

        <div class="flex items-center gap-3">
          <router-link
            :to="`/organizations/${organizationId}/questions`"
            class="inline-flex items-center gap-2 px-4 py-2 text-xs sm:text-sm font-semibold rounded-lg bg-primary text-white hover:bg-primary-hover shadow-xs transition"
          >
            <Plus class="h-4 w-4" />
            <span>Manage Question Banks</span>
          </router-link>
        </div>
      </div>

      <!-- Content -->
      <div v-if="loading" class="py-16 text-center">
        <div class="h-8 w-8 animate-spin rounded-full border-2 border-primary border-t-transparent mx-auto mb-3"></div>
        <p class="text-xs sm:text-sm text-text-muted">Loading question banks...</p>
      </div>

      <div v-else-if="banks.length === 0" class="bg-card border border-border rounded-2xl p-12 text-center shadow-xs">
        <div class="h-12 w-12 rounded-2xl bg-surface-raised border border-border mx-auto flex items-center justify-center text-text-muted mb-4">
          <Layers class="h-6 w-6" />
        </div>
        <h3 class="text-base sm:text-lg font-semibold text-text-strong">No Question Banks Created</h3>
        <p class="text-xs sm:text-sm text-text-muted max-w-md mx-auto mt-1 mb-6">
          Create question banks with single choice, multiple choice, true/false, or structured questions to use during hiring stages.
        </p>
        <router-link
          :to="`/organizations/${organizationId}/questions?create=true`"
          class="inline-flex items-center gap-2 px-4 py-2 text-xs sm:text-sm font-semibold rounded-lg bg-primary text-white hover:bg-primary-hover shadow-xs transition"
        >
          <Plus class="h-4 w-4" />
          <span>Create Question Bank</span>
        </router-link>
      </div>

      <div v-else class="space-y-4">
        <div class="flex items-center justify-between">
          <h2 class="text-sm font-bold text-text-strong uppercase tracking-wider">
            Available Banks ({{ banks.length }})
          </h2>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div
            v-for="b in banks"
            :key="b.id"
            @click="router.push(`/organizations/${organizationId}/questions`)"
            class="bg-card border border-border hover:border-primary/50 hover:bg-surface-raised rounded-2xl p-5 shadow-xs transition cursor-pointer space-y-3"
          >
            <div class="flex items-start justify-between gap-3">
              <div class="min-w-0 flex-1">
                <div class="flex items-center gap-2 flex-wrap">
                  <h3 class="text-base font-bold text-text-strong">{{ b.name }}</h3>
                  <span v-if="b.category" class="px-2 py-0.5 text-[10px] font-semibold rounded-md bg-surface-raised border border-border text-text-muted">
                    {{ b.category }}
                  </span>
                </div>
                <p v-if="b.description" class="text-xs text-text-secondary mt-1 line-clamp-2">
                  {{ b.description }}
                </p>
              </div>
              <ChevronRight class="h-5 w-5 text-text-subtle shrink-0 mt-0.5" />
            </div>

            <div class="pt-3 border-t border-border flex items-center justify-between text-xs text-text-muted">
              <span>{{ b.questions_count }} questions · {{ b.total_points }} pts</span>
              <span class="font-medium text-text-secondary">
                {{ b.creator_name ? `By ${b.creator_name}` : '' }}
              </span>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
