import { reactive } from 'vue'

export interface ToastItem {
  id: string
  title: string
  detail?: string
  variant: 'success' | 'info' | 'warning'
}

let nextId = 0

const toasts = reactive<ToastItem[]>([])

function addToast(toast: Omit<ToastItem, 'id'>) {
  const id = `toast-${++nextId}`
  toasts.push({ ...toast, id })
  setTimeout(() => dismissToast(id), 4000)
}

function dismissToast(id: string) {
  const idx = toasts.findIndex(t => t.id === id)
  if (idx !== -1) toasts.splice(idx, 1)
}

export function useToast() {
  return { toasts, addToast, dismissToast }
}
