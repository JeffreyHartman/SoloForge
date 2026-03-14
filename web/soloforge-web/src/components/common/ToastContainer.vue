<script setup lang="ts">
import { useToast } from '../../composables/useToast'

const { toasts, dismissToast } = useToast()
</script>

<template>
  <div class="fixed bottom-4 right-4 z-50 flex flex-col gap-2" aria-live="polite">
    <TransitionGroup
      enter-active-class="transition duration-200 ease-out"
      enter-from-class="translate-y-2 opacity-0"
      enter-to-class="translate-y-0 opacity-100"
      leave-active-class="transition duration-150 ease-in"
      leave-from-class="translate-y-0 opacity-100"
      leave-to-class="translate-y-2 opacity-0"
    >
      <div
        v-for="toast in toasts"
        :key="toast.id"
        class="flex items-start gap-2 rounded-xl border bg-[var(--color-bg-card-solid)] px-4 py-3 shadow-lg"
        :class="
          toast.variant === 'success'
            ? 'border-[var(--color-border-success)]'
            : toast.variant === 'warning'
              ? 'border-[var(--color-border-warning)]'
              : 'border-[var(--color-border-primary)]'
        "
      >
        <div class="min-w-0 flex-1">
          <div class="text-sm font-semibold text-[var(--color-text-primary)]">{{ toast.title }}</div>
          <div v-if="toast.detail" class="mt-0.5 text-xs text-[var(--color-text-muted)]">{{ toast.detail }}</div>
        </div>
        <button
          type="button"
          class="shrink-0 rounded-lg p-1 text-[var(--color-text-dimmed)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)] transition"
          aria-label="Dismiss notification"
          @click="dismissToast(toast.id)"
        >
          <svg class="h-3.5 w-3.5" viewBox="0 0 14 14" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M2 2l10 10M12 2L2 12" />
          </svg>
        </button>
      </div>
    </TransitionGroup>
  </div>
</template>
