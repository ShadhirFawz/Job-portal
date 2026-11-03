<script setup lang="ts">
import { ref, watch, computed, nextTick } from 'vue'
import {
  AlertTriangle,
  Trash2,
  Info,
  HelpCircle,
  X
} from 'lucide-vue-next'
import { useConfirm, type ConfirmVariant } from '@/composables/useConfirm'

const props = withDefaults(
  defineProps<{
    open?: boolean
    title?: string
    message?: string
    confirmLabel?: string
    cancelLabel?: string
    variant?: ConfirmVariant
    danger?: boolean
  }>(),
  {
    open: undefined,
    title: undefined,
    message: undefined,
    confirmLabel: undefined,
    cancelLabel: undefined,
    variant: undefined,
    danger: undefined,
  }
)

const emit = defineEmits<{
  (e: 'confirm'): void
  (e: 'cancel'): void
}>()

const { confirmState, handleConfirm, handleCancel } = useConfirm()

const isOpen = computed(() => {
  return props.open !== undefined ? props.open : confirmState.value.open
})

const currentTitle = computed(() => {
  return props.title ?? confirmState.value.title ?? 'Confirm Action'
})

const currentMessage = computed(() => {
  return props.message ?? confirmState.value.message ?? 'Are you sure you want to proceed?'
})

const currentConfirmLabel = computed(() => {
  return props.confirmLabel ?? confirmState.value.confirmLabel ?? 'Confirm'
})

const currentCancelLabel = computed(() => {
  return props.cancelLabel ?? confirmState.value.cancelLabel ?? 'Cancel'
})

const currentVariant = computed<ConfirmVariant>(() => {
  if (props.variant) return props.variant
  if (props.danger) return 'danger'
  if (confirmState.value.variant) return confirmState.value.variant
  if (confirmState.value.danger) return 'danger'
  return 'primary'
})

const panel = ref<HTMLElement | null>(null)

watch(isOpen, async (open) => {
  if (open) {
    await nextTick()
    panel.value?.focus()
  }
})

function onKeydown(e: KeyboardEvent) {
  if (e.key === 'Escape') {
    closeCancel()
  }
}

function closeConfirm() {
  if (props.open !== undefined) {
    emit('confirm')
  } else {
    handleConfirm()
  }
}

function closeCancel() {
  if (props.open !== undefined) {
    emit('cancel')
  } else {
    handleCancel()
  }
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
        v-if="isOpen"
        class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-xs"
        role="dialog"
        aria-modal="true"
        aria-labelledby="confirm-dialog-title"
        @click.self="closeCancel"
        @keydown="onKeydown"
      >
        <div
          ref="panel"
          tabindex="-1"
          class="relative w-full max-w-md rounded-2xl bg-[var(--bg-card)] border border-[var(--border-color)] shadow-2xl p-6 focus:outline-hidden transition-all transform animate-in fade-in zoom-in-95 duration-150"
        >
          <!-- Close button -->
          <button
            type="button"
            @click="closeCancel"
            class="absolute top-4 right-4 p-1 rounded-lg text-[var(--text-muted)] hover:text-[var(--text-main)] hover:bg-[var(--bg-surface-raised)] transition"
            aria-label="Close dialog"
          >
            <X class="w-4 h-4" />
          </button>

          <div class="flex items-start gap-4">
            <!-- Icon Badge based on Variant -->
            <div
              class="flex-shrink-0 w-11 h-11 rounded-xl flex items-center justify-center shadow-xs"
              :class="{
                'bg-rose-500/10 text-rose-600 dark:text-rose-400 border border-rose-500/20': currentVariant === 'danger',
                'bg-amber-500/10 text-amber-600 dark:text-amber-400 border border-amber-500/20': currentVariant === 'warning',
                'bg-sky-500/10 text-sky-600 dark:text-sky-400 border border-sky-500/20': currentVariant === 'info',
                'bg-indigo-500/10 text-indigo-600 dark:text-indigo-400 border border-indigo-500/20': currentVariant === 'primary'
              }"
            >
              <Trash2 v-if="currentVariant === 'danger'" class="w-5 h-5" />
              <AlertTriangle v-else-if="currentVariant === 'warning'" class="w-5 h-5" />
              <Info v-else-if="currentVariant === 'info'" class="w-5 h-5" />
              <HelpCircle v-else class="w-5 h-5" />
            </div>

            <!-- Content -->
            <div class="flex-1 pr-4">
              <h3
                id="confirm-dialog-title"
                class="text-base font-semibold text-[var(--text-main)] leading-6"
              >
                {{ currentTitle }}
              </h3>
              <p class="mt-2 text-sm text-[var(--text-muted)] leading-relaxed whitespace-pre-line">
                {{ currentMessage }}
              </p>
            </div>
          </div>

          <!-- Actions -->
          <div class="mt-6 flex items-center justify-end gap-3 pt-3 border-t border-[var(--border-color)]">
            <button
              type="button"
              @click="closeCancel"
              class="px-4 py-2 text-xs sm:text-sm font-medium rounded-lg text-[var(--text-main)] bg-[var(--bg-surface-raised)] hover:bg-[var(--border-color)] border border-[var(--border-color)] focus:outline-hidden focus:ring-2 focus:ring-indigo-500/30 transition cursor-pointer"
            >
              {{ currentCancelLabel }}
            </button>
            <button
              type="button"
              @click="closeConfirm"
              class="inline-flex items-center justify-center px-4 py-2 text-xs sm:text-sm font-semibold rounded-lg text-white shadow-xs focus:outline-hidden focus:ring-2 focus:ring-offset-1 transition cursor-pointer"
              :class="{
                'bg-rose-600 hover:bg-rose-700 focus:ring-rose-500': currentVariant === 'danger',
                'bg-amber-600 hover:bg-amber-700 focus:ring-amber-500': currentVariant === 'warning',
                'bg-sky-600 hover:bg-sky-700 focus:ring-sky-500': currentVariant === 'info',
                'bg-indigo-600 hover:bg-indigo-700 focus:ring-indigo-500': currentVariant === 'primary'
              }"
            >
              {{ currentConfirmLabel }}
            </button>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>
