<script setup lang="ts">
import { ref, watch, nextTick } from 'vue'
import { X, Loader2 } from 'lucide-vue-next'
import type { OrgMember } from '@/types/member'

const props = defineProps<{
  member: OrgMember | null
  saving?: boolean
}>()

const emit = defineEmits<{
  (e: 'close'): void
  (e: 'save', payload: { memberId: string; title: string; department: string }): void
}>()

const editModalEl = ref<HTMLFormElement | null>(null)
const editTitle = ref('')
const editDepartment = ref('')

watch(
  () => props.member,
  (newMember) => {
    if (newMember) {
      editTitle.value = newMember.title || ''
      editDepartment.value = newMember.department || ''
      document.body.style.overflow = 'hidden'
      nextTick(() => editModalEl.value?.focus())
    } else {
      document.body.style.overflow = ''
    }
  },
  { immediate: true }
)

function getDisplayName(m: OrgMember | null): string {
  if (!m) return ''
  if (m.user?.display_name) return m.user.display_name
  if (m.user?.first_name || m.user?.last_name) {
    return `${m.user.first_name || ''} ${m.user.last_name || ''}`.trim()
  }
  return m.user?.email || 'Member'
}

function handleClose() {
  emit('close')
}

function handleSubmit() {
  if (!props.member) return
  emit('save', {
    memberId: props.member.id,
    title: editTitle.value.trim(),
    department: editDepartment.value.trim(),
  })
}
</script>

<template>
  <Teleport to="body">
    <Transition
      enter-active-class="transition ease-out duration-150"
      enter-from-class="opacity-0"
      enter-to-class="opacity-100"
      leave-active-class="transition ease-in duration-100"
      leave-from-class="opacity-100"
      leave-to-class="opacity-0"
    >
      <div
        v-if="member"
        class="fixed inset-0 bg-black/50 backdrop-blur-xs flex items-center justify-center p-4 z-50"
        role="dialog"
        aria-modal="true"
        aria-labelledby="edit-modal-title"
        @click.self="handleClose"
        @keydown.esc="handleClose"
      >
        <form
          ref="editModalEl"
          tabindex="-1"
          @submit.prevent="handleSubmit"
          class="bg-white rounded-2xl shadow-2xl max-w-md w-full p-6 space-y-4 focus:outline-hidden"
        >
          <div class="flex items-center justify-between border-b pb-3 border-gray-100">
            <h3 id="edit-modal-title" class="text-base font-bold text-gray-900">
              Edit Team Member
            </h3>
            <button
              type="button"
              @click="handleClose"
              class="p-1 text-gray-400 hover:text-gray-700 rounded transition"
              aria-label="Close"
            >
              <X class="h-5 w-5" />
            </button>
          </div>

          <p class="text-xs text-gray-500">
            Updating details for <strong>{{ getDisplayName(member) }}</strong>
          </p>

          <div>
            <label class="block text-xs font-semibold text-gray-700 uppercase mb-1">
              Job Title / Position
            </label>
            <input
              v-model="editTitle"
              type="text"
              placeholder="e.g. Senior Recruiter, HR Director"
              class="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 shadow-xs"
            />
          </div>

          <div>
            <label class="block text-xs font-semibold text-gray-700 uppercase mb-1">
              Department
            </label>
            <input
              v-model="editDepartment"
              type="text"
              placeholder="e.g. Human Resources, Engineering"
              class="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 shadow-xs"
            />
          </div>

          <div class="flex justify-end gap-2 pt-3 border-t border-gray-100">
            <button
              type="button"
              @click="handleClose"
              class="px-4 py-2 text-xs font-medium text-gray-700 hover:bg-gray-100 rounded-lg transition"
            >
              Cancel
            </button>
            <button
              type="submit"
              :disabled="saving"
              class="inline-flex items-center gap-1.5 px-4 py-2 text-xs font-semibold text-white bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 rounded-lg shadow-xs transition"
            >
              <Loader2 v-if="saving" class="h-3.5 w-3.5 animate-spin" />
              {{ saving ? 'Saving…' : 'Save Changes' }}
            </button>
          </div>
        </form>
      </div>
    </Transition>
  </Teleport>
</template>

