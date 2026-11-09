import { ref } from 'vue'

export type ConfirmVariant = 'danger' | 'warning' | 'info' | 'primary'

export interface ConfirmOptions {
  title: string
  message: string
  confirmLabel?: string
  cancelLabel?: string
  variant?: ConfirmVariant
  danger?: boolean
}

interface ConfirmState extends ConfirmOptions {
  open: boolean
  resolve?: ((value: boolean) => void) | null
}

const state = ref<ConfirmState>({
  open: false,
  title: '',
  message: '',
  confirmLabel: 'Confirm',
  cancelLabel: 'Cancel',
  variant: 'primary',
  resolve: null,
})

export function useConfirm() {
  function confirm(options: ConfirmOptions): Promise<boolean> {
    return new Promise((resolve) => {
      const isDanger = options.danger || options.variant === 'danger'
      state.value = {
        open: true,
        title: options.title,
        message: options.message,
        confirmLabel: options.confirmLabel || (isDanger ? 'Delete' : 'Confirm'),
        cancelLabel: options.cancelLabel || 'Cancel',
        variant: options.variant || (isDanger ? 'danger' : 'primary'),
        danger: isDanger,
        resolve,
      }
    })
  }

  function handleConfirm() {
    if (state.value.resolve) {
      state.value.resolve(true)
    }
    state.value.open = false
    state.value.resolve = null
  }

  function handleCancel() {
    if (state.value.resolve) {
      state.value.resolve(false)
    }
    state.value.open = false
    state.value.resolve = null
  }

  return {
    confirmState: state,
    confirm,
    handleConfirm,
    handleCancel,
  }
}

