<script setup lang="ts">
import type { ViewName } from '../../types'

defineProps<{
  currentView: ViewName
}>()

defineEmits<{
  navigate: [view: ViewName]
}>()

const tabs: { id: ViewName; label: string; icon: string }[] = [
  { id: 'dashboard', label: 'Dashboard', icon: 'M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6' },
  { id: 'tools', label: 'Tools', icon: 'M19.428 15.428a2 2 0 00-1.022-.547l-2.387-.477a6 6 0 00-3.86.517l-.318.158a6 6 0 01-3.86.517L6.05 15.21a2 2 0 00-1.806.547M8 4h8l-1 1v5.172a2 2 0 00.586 1.414l5 5c1.26 1.26.367 3.414-1.415 3.414H4.828c-1.782 0-2.674-2.154-1.414-3.414l5-5A2 2 0 009 10.172V5L8 4z' },
  { id: 'adventure', label: 'Adventure', icon: 'M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z' },
  { id: 'journal', label: 'Journal', icon: 'M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253' },
  { id: 'history', label: 'History', icon: 'M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z' },
]
</script>

<template>
  <nav class="mx-auto max-w-6xl px-4 pt-6">
    <div class="flex gap-1 rounded-2xl border border-[var(--color-border-secondary)] bg-[var(--color-bg-card)] p-1 shadow-sm backdrop-blur">
      <button
        v-for="tab in tabs"
        :key="tab.id"
        type="button"
        class="flex flex-1 items-center justify-center gap-2 rounded-xl px-3 py-2 text-sm font-medium transition"
        :class="
          currentView === tab.id
            ? 'bg-[var(--color-bg-accent)] text-[var(--color-text-inverted)] shadow-sm'
            : 'text-[var(--color-text-muted)] hover:bg-[var(--color-bg-hover)] hover:text-[var(--color-text-primary)]'
        "
        @click="$emit('navigate', tab.id)"
      >
        <svg class="hidden h-4 w-4 sm:block" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" :d="tab.icon" />
        </svg>
        <span>{{ tab.label }}</span>
      </button>
    </div>
  </nav>
</template>
