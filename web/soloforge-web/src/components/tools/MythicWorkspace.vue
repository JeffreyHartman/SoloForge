<script setup lang="ts">
import FateCheckPanel from '../mythic/FateCheckPanel.vue'
import SceneCheckPanel from '../mythic/SceneCheckPanel.vue'
import RandomEventPanel from '../mythic/RandomEventPanel.vue'
import MeaningPanel from '../mythic/MeaningPanel.vue'
import DiceRollerPanel from '../mythic/DiceRollerPanel.vue'
import { useToolbarPrefs } from '../../composables/useToolbarPrefs'

const { isPinned, pinTool, unpinTool } = useToolbarPrefs()

function togglePin(toolId: string) {
  if (isPinned(toolId)) {
    unpinTool(toolId)
  } else {
    pinTool(toolId)
  }
}
</script>

<template>
  <div class="grid grid-cols-1 gap-6 lg:grid-cols-2">
    <div class="space-y-6">
      <FateCheckPanel>
        <template #pin>
          <button
            type="button"
            class="shrink-0 rounded-lg p-1 transition"
            :class="isPinned('fate-check') ? 'text-[var(--color-text-warning)]' : 'text-[var(--color-text-dimmed)] hover:text-[var(--color-text-primary)]'"
            :aria-label="isPinned('fate-check') ? 'Unpin Fate Check from toolbar' : 'Pin Fate Check to toolbar'"
            :aria-pressed="isPinned('fate-check')"
            @click="togglePin('fate-check')"
          >
            <svg class="h-4 w-4" viewBox="0 0 16 16" :fill="isPinned('fate-check') ? 'currentColor' : 'none'" stroke="currentColor" stroke-width="1.5" aria-hidden="true">
              <path d="M8 1l2.1 4.3 4.7.7-3.4 3.3.8 4.7L8 11.8 3.8 14l.8-4.7L1.2 6l4.7-.7z" />
            </svg>
          </button>
        </template>
      </FateCheckPanel>
      <SceneCheckPanel>
        <template #pin>
          <button
            type="button"
            class="shrink-0 rounded-lg p-1 transition"
            :class="isPinned('scene-check') ? 'text-[var(--color-text-warning)]' : 'text-[var(--color-text-dimmed)] hover:text-[var(--color-text-primary)]'"
            :aria-label="isPinned('scene-check') ? 'Unpin Scene Check from toolbar' : 'Pin Scene Check to toolbar'"
            :aria-pressed="isPinned('scene-check')"
            @click="togglePin('scene-check')"
          >
            <svg class="h-4 w-4" viewBox="0 0 16 16" :fill="isPinned('scene-check') ? 'currentColor' : 'none'" stroke="currentColor" stroke-width="1.5">
              <path d="M8 1l2.1 4.3 4.7.7-3.4 3.3.8 4.7L8 11.8 3.8 14l.8-4.7L1.2 6l4.7-.7z" />
            </svg>
          </button>
        </template>
      </SceneCheckPanel>
      <RandomEventPanel>
        <template #pin>
          <button
            type="button"
            class="shrink-0 rounded-lg p-1 transition"
            :class="isPinned('random-event') ? 'text-[var(--color-text-warning)]' : 'text-[var(--color-text-dimmed)] hover:text-[var(--color-text-primary)]'"
            :aria-label="isPinned('random-event') ? 'Unpin Random Event from toolbar' : 'Pin Random Event to toolbar'"
            :aria-pressed="isPinned('random-event')"
            @click="togglePin('random-event')"
          >
            <svg class="h-4 w-4" viewBox="0 0 16 16" :fill="isPinned('random-event') ? 'currentColor' : 'none'" stroke="currentColor" stroke-width="1.5">
              <path d="M8 1l2.1 4.3 4.7.7-3.4 3.3.8 4.7L8 11.8 3.8 14l.8-4.7L1.2 6l4.7-.7z" />
            </svg>
          </button>
        </template>
      </RandomEventPanel>
    </div>
    <div class="space-y-6">
      <MeaningPanel>
        <template #pin>
          <button
            type="button"
            class="shrink-0 rounded-lg p-1 transition"
            :class="isPinned('meaning') ? 'text-[var(--color-text-warning)]' : 'text-[var(--color-text-dimmed)] hover:text-[var(--color-text-primary)]'"
            :aria-label="isPinned('meaning') ? 'Unpin Meaning from toolbar' : 'Pin Meaning to toolbar'"
            :aria-pressed="isPinned('meaning')"
            @click="togglePin('meaning')"
          >
            <svg class="h-4 w-4" viewBox="0 0 16 16" :fill="isPinned('meaning') ? 'currentColor' : 'none'" stroke="currentColor" stroke-width="1.5">
              <path d="M8 1l2.1 4.3 4.7.7-3.4 3.3.8 4.7L8 11.8 3.8 14l.8-4.7L1.2 6l4.7-.7z" />
            </svg>
          </button>
        </template>
      </MeaningPanel>
      <DiceRollerPanel>
        <template #pin>
          <button
            type="button"
            class="shrink-0 rounded-lg p-1 transition"
            :class="isPinned('dice-roller') ? 'text-[var(--color-text-warning)]' : 'text-[var(--color-text-dimmed)] hover:text-[var(--color-text-primary)]'"
            :aria-label="isPinned('dice-roller') ? 'Unpin Dice Roller from toolbar' : 'Pin Dice Roller to toolbar'"
            :aria-pressed="isPinned('dice-roller')"
            @click="togglePin('dice-roller')"
          >
            <svg class="h-4 w-4" viewBox="0 0 16 16" :fill="isPinned('dice-roller') ? 'currentColor' : 'none'" stroke="currentColor" stroke-width="1.5">
              <path d="M8 1l2.1 4.3 4.7.7-3.4 3.3.8 4.7L8 11.8 3.8 14l.8-4.7L1.2 6l4.7-.7z" />
            </svg>
          </button>
        </template>
      </DiceRollerPanel>
    </div>
  </div>
</template>
