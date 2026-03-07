<script setup lang="ts">
defineProps<{
  title?: string
  subtitle?: string
}>()
</script>

<template>
  <div class="rounded-3xl border border-[var(--color-border-secondary)] bg-[var(--color-bg-card)] p-5 shadow-sm backdrop-blur">
    <div v-if="title || $slots.header" class="flex items-center justify-between gap-3">
      <div v-if="title">
        <h2 class="text-sm font-semibold tracking-tight text-[var(--color-text-primary)]">{{ title }}</h2>
        <p v-if="subtitle" class="text-xs text-[var(--color-text-muted)]">{{ subtitle }}</p>
      </div>
      <slot name="header" />
    </div>
    <div :class="{ 'mt-4': title || $slots.header }">
      <slot />
    </div>
  </div>
</template>
