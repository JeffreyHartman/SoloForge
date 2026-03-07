<script setup lang="ts">
import { computed } from 'vue'
import DOMPurify from 'dompurify'
import type { RollSegment } from '../../composables/useJournalParser'

const props = defineProps<{
  segment: RollSegment
  collapsed: boolean
}>()

defineEmits<{
  toggle: []
  delete: []
}>()

function sanitize(html: string): string {
  return DOMPurify.sanitize(html)
}

interface RollStyle {
  border: string
  color: string
  bg: string
  label: string
}

const STYLES: Record<string, RollStyle> = {
  'Fate Check':    { border: '#3b82f6', color: '#3b82f6', bg: 'rgba(59, 130, 246, 0.1)',  label: 'Fate' },
  'Scene Check':   { border: '#f59e0b', color: '#d97706', bg: 'rgba(245, 158, 11, 0.1)',  label: 'Scene' },
  'Random Event':  { border: '#a855f7', color: '#9333ea', bg: 'rgba(168, 85, 247, 0.1)',  label: 'Event' },
  'Meaning Roll':  { border: '#10b981', color: '#059669', bg: 'rgba(16, 185, 129, 0.1)',  label: 'Meaning' },
  'Dice Roll':     { border: '#f43f5e', color: '#e11d48', bg: 'rgba(244, 63, 94, 0.1)',   label: 'Dice' },
  'Note':          { border: '#94a3b8', color: '#64748b', bg: 'rgba(148, 163, 184, 0.1)', label: 'Note' },
}

const style = computed(() => STYLES[props.segment.rollType] ?? STYLES['Note'])

const summary = computed(() => {
  const f = props.segment.fields
  const type = props.segment.rollType
  const result = f.Result ?? ''

  if (type === 'Fate Check')    return { context: f.Question ?? '', result }
  if (type === 'Scene Check')   return { context: f.Context ?? '', result }
  if (type === 'Meaning Roll')  return { context: f.For ?? '', result }
  if (type === 'Dice Roll')     return { context: f.Expression ?? '', result: f.Total ?? result }
  if (type === 'Random Event')  return { context: '', result: f.Event ?? result }
  if (type === 'Note')          return { context: '', result: f.Note ?? '' }
  return { context: '', result: Object.values(f)[0] ?? '' }
})

const fieldEntries = computed(() => Object.entries(props.segment.fields))
</script>

<template>
  <div class="group/roll relative my-2">
    <!-- Collapsed -->
    <div
      v-if="collapsed"
      role="button"
      tabindex="0"
      :aria-expanded="false"
      class="flex items-center gap-2 rounded-lg px-3 py-1.5 cursor-pointer select-none transition-colors hover:bg-[var(--color-bg-hover)] focus-visible:outline focus-visible:outline-2 focus-visible:outline-[var(--color-bg-accent)]"
      :style="{ borderLeft: `3px solid ${style.border}` }"
      @click="$emit('toggle')"
      @keydown.enter.prevent="$emit('toggle')"
      @keydown.space.prevent="$emit('toggle')"
    >
      <span
        class="shrink-0 rounded px-1.5 py-0.5 text-[10px] font-bold uppercase tracking-wider"
        :style="{ color: style.color, backgroundColor: style.bg }"
      >
        {{ style.label }}
      </span>
      <span class="truncate text-sm">
        <span v-if="summary.context" class="mr-1.5 font-semibold text-[var(--color-text-primary)]">{{ summary.context }}</span>
        <span class="text-[var(--color-text-secondary)]">{{ summary.result }}</span>
      </span>
      <div class="ml-auto flex items-center gap-1 shrink-0 opacity-0 group-hover/roll:opacity-100 group-focus-within/roll:opacity-100 transition-opacity">
        <button
          class="rounded p-0.5 text-[var(--color-text-dimmed)] hover:text-[var(--color-text-danger)] hover:bg-[var(--color-bg-danger)] transition"
          title="Remove from journal"
          @click.stop="$emit('delete')"
        >
          <svg class="h-3.5 w-3.5" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M4 4l8 8M12 4l-8 8" />
          </svg>
        </button>
      </div>

      <!-- Hover detail panel -->
      <div class="invisible group-hover/roll:visible group-focus-within/roll:visible absolute left-8 top-full z-20 mt-1 w-80 rounded-xl border border-[var(--color-border-primary)] bg-[var(--color-bg-card-solid)] p-3 shadow-lg transition-all">
        <div
          class="mb-2 text-[11px] font-bold uppercase tracking-wider"
          :style="{ color: style.color }"
        >
          {{ segment.rollType }}
        </div>
        <div v-for="[key, val] in fieldEntries" :key="key" class="flex gap-2 py-0.5 text-sm">
          <span class="shrink-0 font-semibold text-[var(--color-text-muted)]">{{ key }}:</span>
          <span class="text-[var(--color-text-primary)]" v-html="sanitize(val)" />
        </div>
      </div>
    </div>

    <!-- Expanded -->
    <div
      v-else
      class="rounded-xl border border-[var(--color-border-primary)] bg-[var(--color-bg-card)] px-4 py-3 shadow-sm"
      :style="{ borderLeft: `4px solid ${style.border}` }"
    >
      <div class="mb-2 flex items-center justify-between">
        <div class="flex items-center gap-2">
          <span
            class="rounded-md px-2 py-0.5 text-[11px] font-bold uppercase tracking-wider"
            :style="{ color: style.color, backgroundColor: style.bg }"
          >
            {{ style.label }}
          </span>
          <span class="text-xs text-[var(--color-text-dimmed)]">{{ segment.rollType }}</span>
        </div>
        <div class="flex items-center gap-1">
          <button
            class="rounded p-1 text-[var(--color-text-dimmed)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)] transition"
            title="Collapse"
            :aria-expanded="true"
            @click="$emit('toggle')"
          >
            <svg class="h-4 w-4" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M4 10l4-4 4 4" />
            </svg>
          </button>
          <button
            class="rounded p-1 text-[var(--color-text-dimmed)] hover:text-[var(--color-text-danger)] hover:bg-[var(--color-bg-danger)] transition"
            title="Remove from journal"
            @click="$emit('delete')"
          >
            <svg class="h-4 w-4" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M4 4l8 8M12 4l-8 8" />
            </svg>
          </button>
        </div>
      </div>

      <div class="space-y-1">
        <div v-for="[key, val] in fieldEntries" :key="key" class="flex gap-3 text-sm">
          <span class="shrink-0 min-w-20 font-semibold text-[var(--color-text-muted)]">{{ key }}</span>
          <span class="text-[var(--color-text-primary)]" v-html="sanitize(val)" />
        </div>
      </div>
    </div>
  </div>
</template>
