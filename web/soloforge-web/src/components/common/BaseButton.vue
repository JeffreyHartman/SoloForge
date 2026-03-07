<script setup lang="ts">
defineProps<{
  variant?: 'primary' | 'secondary' | 'success' | 'warning' | 'danger' | 'ghost'
  size?: 'sm' | 'md' | 'lg'
  disabled?: boolean
  loading?: boolean
}>()

defineEmits<{
  click: [event: MouseEvent]
}>()
</script>

<template>
  <button
    type="button"
    class="inline-flex items-center justify-center gap-2 font-semibold shadow-sm transition disabled:opacity-50"
    :class="[
      // Size classes
      size === 'sm' ? 'rounded-xl px-3 py-1.5 text-xs' :
      size === 'lg' ? 'rounded-2xl px-5 py-3 text-base' :
      'rounded-xl px-4 py-2 text-sm',

      // Variant classes
      variant === 'secondary' ? 'border border-[var(--color-border-primary)] bg-[var(--color-bg-card-solid)] text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)]' :
      variant === 'success' ? 'bg-[var(--color-bg-success)] text-white hover:bg-[var(--color-bg-success-hover)]' :
      variant === 'warning' ? 'bg-[var(--color-bg-warning)] text-[var(--color-text-inverted)] hover:bg-[var(--color-bg-warning-hover)]' :
      variant === 'danger' ? 'border border-[var(--color-border-danger)] bg-[var(--color-bg-danger)] text-[var(--color-text-danger)] hover:bg-[var(--color-bg-danger-hover)]' :
      variant === 'ghost' ? 'bg-transparent text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)]' :
      'bg-[var(--color-bg-accent)] text-[var(--color-text-inverted)] hover:bg-[var(--color-bg-accent-hover)]'
    ]"
    :disabled="disabled || loading"
    :aria-busy="loading"
    aria-live="polite"
    @click="$emit('click', $event)"
  >
    <span v-if="loading" class="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" aria-hidden="true" />
    <span v-if="loading" class="sr-only">Loading…</span>
    <slot />
  </button>
</template>
