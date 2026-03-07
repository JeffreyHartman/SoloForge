<script setup lang="ts">
import type { HistoryEntry } from '../../types'
import BaseCard from '../common/BaseCard.vue'
import BaseButton from '../common/BaseButton.vue'

defineProps<{
  entries: HistoryEntry[]
  loading: boolean
  apiOnline: boolean
}>()

defineEmits<{
  refresh: []
}>()

function formatDate(value: string | null | undefined): string {
  if (!value) return ''
  const d = new Date(value)
  if (Number.isNaN(d.getTime())) return value
  return d.toLocaleString()
}
</script>

<template>
  <BaseCard title="History">
    <template #header>
      <BaseButton
        variant="secondary"
        size="sm"
        :disabled="loading || !apiOnline"
        @click="$emit('refresh')"
      >
        Refresh
      </BaseButton>
    </template>

    <div class="max-h-[calc(100vh-16rem)] overflow-auto rounded-2xl border border-[var(--color-border-card)] bg-[var(--color-bg-card-solid)]">
      <div v-if="loading" class="p-4 text-sm text-[var(--color-text-muted)]">Loading history...</div>
      <div v-else-if="entries.length === 0" class="p-4 text-sm text-[var(--color-text-muted)]">No history yet.</div>
      <ul v-else class="divide-y divide-[var(--color-border-primary)]">
        <li v-for="e in entries" :key="e.id" class="p-3">
          <div class="flex items-start justify-between gap-3">
            <div class="min-w-0">
              <div class="text-xs font-semibold text-[var(--color-text-secondary)]">{{ e.type }}</div>
              <div class="mt-0.5 text-sm font-semibold text-[var(--color-text-primary)]">{{ e.result }}</div>
              <div v-if="e.context" class="mt-0.5 text-xs text-[var(--color-text-muted)]">{{ e.context }}</div>
              <div v-if="e.details" class="mt-1 text-xs text-[var(--color-text-dimmed)]">{{ e.details }}</div>
            </div>
            <div class="shrink-0 text-[11px] text-[var(--color-text-dimmed)]">{{ formatDate(e.timestamp) }}</div>
          </div>
        </li>
      </ul>
    </div>
  </BaseCard>
</template>
