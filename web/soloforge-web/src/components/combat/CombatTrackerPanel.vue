<script setup lang="ts">
import { ref, computed } from 'vue'
import { useCombatTracker } from '../../composables/useCombatTracker'
import CombatantRow from './CombatantRow.vue'
import TurnControls from './TurnControls.vue'

defineProps<{
  compact?: boolean
}>()

const tracker = useCombatTracker()
const dragIndex = ref<number | null>(null)
const showClearConfirm = ref(false)
const nameQuery = ref('')

const suggestions = computed(() => {
  if (!nameQuery.value) return []
  return tracker.getCharacterSuggestions(nameQuery.value)
})

function handleDragStart(idx: number, e: DragEvent) {
  dragIndex.value = idx
  if (e.dataTransfer) {
    e.dataTransfer.effectAllowed = 'move'
    e.dataTransfer.setData('text/plain', String(idx))
  }
}

function handleDragOver(_idx: number, e: DragEvent) {
  e.preventDefault()
  if (e.dataTransfer) e.dataTransfer.dropEffect = 'move'
}

function handleDrop(idx: number) {
  if (dragIndex.value !== null && dragIndex.value !== idx) {
    tracker.reorder(dragIndex.value, idx)
  }
  dragIndex.value = null
}

function handleDragEnd() {
  dragIndex.value = null
}

function confirmClear() {
  tracker.clearAll()
  showClearConfirm.value = false
}
</script>

<template>
  <div class="flex h-full flex-col gap-3" :class="compact ? 'p-3' : 'p-0'">
    <!-- Header controls -->
    <div class="flex flex-wrap items-center gap-2">
      <TurnControls
        :round="tracker.round.value"
        :started="tracker.started.value"
        @next="tracker.nextTurn()"
        @prev="tracker.prevTurn()"
        @update:round="tracker.setRound($event)"
      />

      <div class="flex flex-1 flex-wrap items-center justify-end gap-1.5">
        <button
          type="button"
          class="rounded-lg border border-[var(--color-border-primary)] px-2.5 py-1 text-xs font-medium text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)] transition"
          aria-label="Roll All initiative"
          @click="tracker.rollAllInitiative()"
        >
          Roll All
        </button>
        <button
          type="button"
          class="rounded-lg border border-[var(--color-border-primary)] px-2.5 py-1 text-xs font-medium text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)] transition"
          aria-label="Sort by initiative"
          @click="tracker.sortByInitiative()"
        >
          Sort
        </button>
        <button
          type="button"
          class="rounded-lg border border-[var(--color-border-danger)] px-2.5 py-1 text-xs font-medium text-[var(--color-text-danger)] hover:bg-[var(--color-bg-danger-hover)] transition"
          aria-label="Clear combat"
          @click="showClearConfirm = true"
        >
          Clear
        </button>
      </div>
    </div>

    <!-- Header row -->
    <div v-if="tracker.combatants.value.length > 0" class="flex items-center px-2 text-[10px] font-semibold uppercase tracking-wider text-[var(--color-text-muted)]">
      <div class="w-5 shrink-0" />
      <div class="ml-1 w-10 shrink-0 text-center">Type</div>
      <div class="ml-1.5 w-[4.5rem] shrink-0 text-center">Init</div>
      <div class="ml-1.5 min-w-0 flex-1">Name</div>
      <div class="ml-1.5 w-36 shrink-0 text-center">HP</div>
      <div class="ml-1.5 w-12 shrink-0 text-center">AC</div>
      <div v-if="!(compact ?? false)" class="ml-1.5 min-w-0 flex-1">Conditions</div>
      <div class="ml-1 w-7 shrink-0" />
      <div class="ml-0.5 w-6 shrink-0" />
    </div>

    <!-- Combatant list -->
    <div class="flex min-h-0 flex-1 flex-col gap-1 overflow-y-auto" role="list" aria-label="Combatants">
      <CombatantRow
        v-for="(combatant, idx) in tracker.combatants.value"
        :key="combatant.id"
        :combatant="combatant"
        :is-active="tracker.activeCombatantId.value === combatant.id"
        :compact="compact ?? false"
        :suggestions="suggestions"
        @update="tracker.updateCombatant"
        @adjust-hp="tracker.adjustHp"
        @roll="tracker.rollInitiative"
        @remove="tracker.removeCombatant"
        @drag-start="(_, e) => handleDragStart(idx, e)"
        @drag-over="(_, e) => handleDragOver(idx, e)"
        @drop="() => handleDrop(idx)"
        @drag-end="handleDragEnd"
        @name-query="nameQuery = $event"
      />

      <!-- Empty state -->
      <div
        v-if="tracker.combatants.value.length === 0"
        class="flex flex-col items-center justify-center gap-2 py-8 text-center"
      >
        <svg class="h-10 w-10 text-[var(--color-text-dimmed)]" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
          <path d="M12 2L2 7l10 5 10-5-10-5zM2 17l10 5 10-5M2 12l10 5 10-5" />
        </svg>
        <p class="text-sm text-[var(--color-text-muted)]">No combatants yet. Add one to get started.</p>
      </div>
    </div>

    <!-- Add combatant button -->
    <button
      type="button"
      class="w-full rounded-lg border border-dashed border-[var(--color-border-primary)] px-3 py-2 text-xs font-medium text-[var(--color-text-muted)] hover:border-[var(--color-text-dimmed)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)] transition"
      aria-label="Add combatant"
      @click="tracker.addCombatant()"
    >
      + Add Combatant
    </button>

    <!-- Clear confirmation dialog -->
    <div
      v-if="showClearConfirm"
      class="fixed inset-0 z-50 flex items-center justify-center bg-black/30 backdrop-blur-sm"
      role="dialog"
      aria-modal="true"
      aria-labelledby="clear-dialog-title"
      tabindex="-1"
      @click.self="showClearConfirm = false"
      @keydown.escape="showClearConfirm = false"
    >
      <div class="w-72 rounded-2xl border border-[var(--color-border-primary)] bg-[var(--color-bg-card-solid)] p-5 shadow-xl">
        <h3 id="clear-dialog-title" class="mb-2 text-sm font-semibold text-[var(--color-text-primary)]">Clear All Combatants?</h3>
        <p class="mb-4 text-xs text-[var(--color-text-muted)]">This will remove all combatants and reset the round counter. This cannot be undone.</p>
        <div class="flex justify-end gap-2">
          <button
            type="button"
            class="rounded-xl px-3 py-1.5 text-sm text-[var(--color-text-muted)] hover:bg-[var(--color-bg-hover)] transition"
            aria-label="Cancel clear"
            @click="showClearConfirm = false"
          >
            Cancel
          </button>
          <button
            type="button"
            class="rounded-xl bg-[var(--color-bg-danger)] px-3 py-1.5 text-sm font-medium text-[var(--color-text-danger)] border border-[var(--color-border-danger)] hover:bg-[var(--color-bg-danger-hover)] transition"
            aria-label="Confirm clear"
            @click="confirmClear"
          >
            Clear
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
