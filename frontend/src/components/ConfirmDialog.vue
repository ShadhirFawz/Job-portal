<script setup lang="ts">
import { ref, watch, nextTick } from 'vue'

const props = defineProps<{
  open: boolean
  title: string
  message: string
  confirmLabel?: string
  cancelLabel?: string
  danger?: boolean
}>()

const emit = defineEmits<{ confirm: []; cancel: [] }>()
const panel = ref<HTMLElement | null>(null)

watch(
  () => props.open,
  async (isOpen) => {
    if (isOpen) {
      await nextTick()
      panel.value?.focus()
    }
  }
)

function onKeydown(e: KeyboardEvent) {
  if (e.key === 'Escape') emit('cancel')
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
        v-if="open"
        class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-sm"
        role="dialog"
        aria-modal="true"
        @click.self="emit('cancel')"
        @keydown="onKeydown"
      >
        <div
          ref="panel"
          tabindex="-1"
          class="bg-white rounded-xl shadow-2xl max-w-sm w-full p-6 focus:outline-none"
        >
          <h3 class="text-base font-semibold text-gray-900">{{ title }}</h3>
          <p class="text-sm text-gray-600 mt-2 leading-relaxed">{{ message }}</p>
          <div class="mt-6 flex justify-end gap-2.5">
            <button
              type="button"
              @click="emit('cancel')"
              class="px-4 py-2 text-sm font-medium rounded-md text-gray-700 hover:bg-gray-100 focus:outline-none focus:ring-2 focus:ring-gray-300"
            >
              {{ cancelLabel || 'Cancel' }}
            </button>
            <button
              type="button"
              @click="emit('confirm')"
              class="px-4 py-2 text-sm font-semibold rounded-md text-white focus:outline-none focus:ring-2 focus:ring-offset-1"
              :class="
                danger
                  ? 'bg-red-600 hover:bg-red-700 focus:ring-red-500'
                  : 'bg-indigo-600 hover:bg-indigo-700 focus:ring-indigo-500'
              "
            >
              {{ confirmLabel || 'Confirm' }}
            </button>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>