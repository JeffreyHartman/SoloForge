<script setup lang="ts">
import { computed } from 'vue'
import { useResultBanner } from '../../composables/useResultBanner'
import type { ResultType } from '../../composables/useResultBanner'

const { lastEvent } = useResultBanner()

const COLORS: Record<ResultType, string> = {
  fate:    'var(--color-roll-fate)',
  scene:   'var(--color-roll-scene)',
  event:   'var(--color-roll-event)',
  meaning: 'var(--color-roll-meaning)',
  dice:    'var(--color-roll-dice)',
}

const LABELS: Record<ResultType, string> = {
  fate: 'Fate',
  scene: 'Scene',
  event: 'Event',
  meaning: 'Meaning',
  dice: 'Dice',
}

const dotColor = computed(() => lastEvent.value ? COLORS[lastEvent.value.type] : undefined)
const label = computed(() => lastEvent.value ? LABELS[lastEvent.value.type] : '')
</script>

<template>
  <Transition
    enter-active-class="transition duration-200 ease-out"
    enter-from-class="opacity-0"
    enter-to-class="opacity-100"
    leave-active-class="transition duration-150 ease-in"
    leave-from-class="opacity-100"
    leave-to-class="opacity-0"
  >
    <div
      v-if="lastEvent"
      class="inline-flex items-center gap-1.5 rounded-full border border-[var(--color-border-secondary)] bg-[var(--color-bg-card)] px-3 py-1.5 text-xs shadow-sm backdrop-blur"
      :title="`${label}: ${lastEvent.title}${lastEvent.detail ? ' — ' + lastEvent.detail : ''}`"
    >
      <span
        class="h-2 w-2 shrink-0 rounded-full"
        :style="{ backgroundColor: dotColor }"
      />
      <span class="font-semibold text-[var(--color-text-muted)]">{{ label }}</span>
      <span class="max-w-36 truncate text-[var(--color-text-secondary)]">{{ lastEvent.title }}</span>
    </div>
  </Transition>
</template>
