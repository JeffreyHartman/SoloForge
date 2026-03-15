<script setup lang="ts">
import type { JournalPrefs } from '../../composables/useJournalPrefs'

defineProps<{
  mode: JournalPrefs['mode']
  split: boolean
  enhanced: boolean
  fontFamily: JournalPrefs['fontFamily']
  fontSize: number
  showCollapseControls: boolean
  autoJournalEvents?: boolean
  autoJournalDiceRolls?: boolean
}>()

defineEmits<{
  'update:mode': [value: JournalPrefs['mode']]
  'update:split': [value: boolean]
  'update:enhanced': [value: boolean]
  'update:fontFamily': [value: JournalPrefs['fontFamily']]
  'update:fontSize': [value: number]
  'update:autoJournalEvents': [value: boolean]
  'update:autoJournalDiceRolls': [value: boolean]
  collapseAll: []
  expandAll: []
}>()

const platform = (navigator as any).userAgentData?.platform ?? navigator.platform ?? ''
const isMac = platform.includes('Mac') || platform.includes('mac')
const mod = isMac ? '\u2318' : 'Ctrl+'
</script>

<template>
  <div class="mb-3 flex flex-wrap items-center gap-2 text-xs">
    <!-- Mode toggle -->
    <div class="inline-flex overflow-hidden rounded-lg border border-[var(--color-border-primary)]">
      <button
        class="px-3 py-1.5 font-medium transition"
        :class="!split && mode === 'edit'
          ? 'bg-[var(--color-bg-accent)] text-[var(--color-text-inverted)]'
          : 'bg-[var(--color-bg-card-solid)] text-[var(--color-text-muted)] hover:bg-[var(--color-bg-hover)]'"
        :aria-pressed="!split && mode === 'edit'"
        :title="`Edit mode (${mod}E)`"
        @click="$emit('update:split', false); $emit('update:mode', 'edit')"
      >
        Edit
      </button>
      <button
        class="px-3 py-1.5 font-medium transition"
        :class="!split && mode === 'preview'
          ? 'bg-[var(--color-bg-accent)] text-[var(--color-text-inverted)]'
          : 'bg-[var(--color-bg-card-solid)] text-[var(--color-text-muted)] hover:bg-[var(--color-bg-hover)]'"
        :aria-pressed="!split && mode === 'preview'"
        :title="`Preview mode (${mod}E)`"
        @click="$emit('update:split', false); $emit('update:mode', 'preview')"
      >
        Preview
      </button>
      <button
        class="px-3 py-1.5 font-medium transition"
        :class="split
          ? 'bg-[var(--color-bg-accent)] text-[var(--color-text-inverted)]'
          : 'bg-[var(--color-bg-card-solid)] text-[var(--color-text-muted)] hover:bg-[var(--color-bg-hover)]'"
        :aria-pressed="split"
        :title="`Split view (${mod}Shift+E)`"
        @click="$emit('update:split', !split)"
      >
        Split
      </button>
    </div>

    <!-- Enhanced rendering toggle (visible when preview is showing) -->
    <label
      v-if="mode === 'preview' || split"
      class="inline-flex cursor-pointer items-center gap-1.5 rounded-lg border border-[var(--color-border-primary)] bg-[var(--color-bg-card-solid)] px-2.5 py-1.5 text-[var(--color-text-muted)] transition hover:bg-[var(--color-bg-hover)]"
    >
      <input
        type="checkbox"
        :checked="enhanced"
        class="accent-[var(--color-bg-accent)]"
        @change="$emit('update:enhanced', ($event.target as HTMLInputElement).checked)"
      />
      <span>Enhanced</span>
    </label>

    <!-- Auto-log toggles -->
    <label
      v-if="autoJournalEvents !== undefined"
      class="inline-flex cursor-pointer items-center gap-1.5 rounded-lg border border-[var(--color-border-primary)] bg-[var(--color-bg-card-solid)] px-2.5 py-1.5 text-[var(--color-text-muted)] transition hover:bg-[var(--color-bg-hover)]"
    >
      <input
        type="checkbox"
        :checked="autoJournalEvents"
        class="accent-[var(--color-bg-accent)]"
        aria-label="Auto-log events to journal"
        @change="$emit('update:autoJournalEvents', ($event.target as HTMLInputElement).checked)"
      />
      <span>Auto-log events</span>
    </label>

    <label
      v-if="autoJournalDiceRolls !== undefined"
      class="inline-flex cursor-pointer items-center gap-1.5 rounded-lg border border-[var(--color-border-primary)] bg-[var(--color-bg-card-solid)] px-2.5 py-1.5 text-[var(--color-text-muted)] transition hover:bg-[var(--color-bg-hover)]"
    >
      <input
        type="checkbox"
        :checked="autoJournalDiceRolls"
        class="accent-[var(--color-bg-accent)]"
        aria-label="Auto-log dice rolls to journal"
        @change="$emit('update:autoJournalDiceRolls', ($event.target as HTMLInputElement).checked)"
      />
      <span>Auto-log dice</span>
    </label>

    <!-- Collapse / Expand all -->
    <template v-if="showCollapseControls">
      <button
        class="rounded-lg px-2 py-1.5 text-[var(--color-text-muted)] transition hover:bg-[var(--color-bg-hover)]"
        @click="$emit('collapseAll')"
      >
        Collapse all
      </button>
      <button
        class="rounded-lg px-2 py-1.5 text-[var(--color-text-muted)] transition hover:bg-[var(--color-bg-hover)]"
        @click="$emit('expandAll')"
      >
        Expand all
      </button>
    </template>

    <!-- Spacer -->
    <div class="flex-1" />

    <!-- Font family -->
    <select
      :value="fontFamily"
      aria-label="Journal font family"
      class="rounded-lg border border-[var(--color-border-primary)] bg-[var(--color-bg-input)] px-2 py-1.5 text-[var(--color-text-primary)] outline-none"
      @change="$emit('update:fontFamily', ($event.target as HTMLSelectElement).value as JournalPrefs['fontFamily'])"
    >
      <option value="mono">Monospace</option>
      <option value="sans">Sans-serif</option>
      <option value="serif">Serif</option>
    </select>

    <!-- Font size -->
    <select
      :value="fontSize"
      aria-label="Journal font size"
      class="rounded-lg border border-[var(--color-border-primary)] bg-[var(--color-bg-input)] px-2 py-1.5 text-[var(--color-text-primary)] outline-none"
      @change="$emit('update:fontSize', Number(($event.target as HTMLSelectElement).value))"
    >
      <option v-for="size in [12, 13, 14, 15, 16, 18, 20]" :key="size" :value="size">
        {{ size }}px
      </option>
    </select>
  </div>
</template>
