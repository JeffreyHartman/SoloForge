<script setup lang="ts">
import { ref, computed } from 'vue'
import type { Combatant, CombatantType } from '../../types'
import HpWidget from './HpWidget.vue'

const props = defineProps<{
  combatant: Combatant
  isActive: boolean
  compact: boolean
  suggestions: string[]
}>()

const emit = defineEmits<{
  update: [id: string, patch: Partial<Combatant>]
  adjustHp: [id: string, delta: number]
  roll: [id: string]
  remove: [id: string]
  dragStart: [index: number, event: DragEvent]
  dragOver: [index: number, event: DragEvent]
  drop: [index: number, event: DragEvent]
  dragEnd: []
  nameQuery: [query: string]
}>()

const showSuggestions = ref(false)
const rowEl = ref<HTMLElement | null>(null)

const isDead = computed(() => props.combatant.status === 'dead')

function toggleType() {
  const newType: CombatantType = props.combatant.type === 'PC' ? 'NPC' : 'PC'
  emit('update', props.combatant.id, { type: newType })
}

function toggleDead() {
  const newStatus = isDead.value ? 'active' : 'dead'
  emit('update', props.combatant.id, { status: newStatus })
}

function handleNameInput(e: Event) {
  const val = (e.target as HTMLInputElement).value
  emit('update', props.combatant.id, { name: val })
  emit('nameQuery', val)
  showSuggestions.value = val.length > 0
}

function selectSuggestion(name: string) {
  emit('update', props.combatant.id, { name })
  showSuggestions.value = false
}

function handleNameBlur() {
  setTimeout(() => { showSuggestions.value = false }, 150)
}

function handleInitiativeInput(e: Event) {
  const raw = (e.target as HTMLInputElement).value
  emit('update', props.combatant.id, { initiative: raw ? Number(raw) : null })
}

function handleAcInput(e: Event) {
  const raw = (e.target as HTMLInputElement).value
  emit('update', props.combatant.id, { ac: raw ? Number(raw) : null })
}

/**
 * Handle Tab key to navigate between tabbable fields in this row.
 * Fields in tab order: Initiative → Name → Current HP → Max HP → AC → Conditions
 */
function handleTab(e: KeyboardEvent) {
  if (e.key !== 'Tab' || !rowEl.value) return

  const tabbables = Array.from(
    rowEl.value.querySelectorAll<HTMLElement>('[data-tab-field]')
  ).sort((a, b) => Number(a.dataset.tabOrder ?? 0) - Number(b.dataset.tabOrder ?? 0))

  const currentIdx = tabbables.indexOf(e.target as HTMLElement)
  if (currentIdx === -1) return

  const nextIdx = e.shiftKey ? currentIdx - 1 : currentIdx + 1

  const next = nextIdx >= 0 && nextIdx < tabbables.length ? tabbables[nextIdx] : undefined
  if (next) {
    e.preventDefault()
    // If it's a click-to-edit button (HP), click to enter edit mode — focus will follow
    if (next.tagName === 'BUTTON' && next.getAttribute('aria-label')?.includes('HP')) {
      next.click()
    } else {
      next.focus()
    }
  }
  // If out of range, let default tab behavior move to next row
}
</script>

<template>
  <div
    ref="rowEl"
    class="group flex items-center rounded-lg border px-2 py-1.5 transition"
    :class="[
      isDead
        ? 'border-[var(--color-border-primary)] opacity-50'
        : isActive
          ? 'border-[var(--color-text-accent)] bg-[var(--color-bg-accent)]/10 shadow-sm'
          : combatant.type === 'PC'
            ? 'border-[var(--color-border-secondary)] bg-[var(--color-bg-accent)]/5'
            : 'border-[var(--color-border-secondary)]',
    ]"
    draggable="true"
    role="listitem"
    :aria-label="`Combatant ${combatant.name || 'unnamed'}`"
    @dragstart="emit('dragStart', 0, $event)"
    @dragover.prevent="emit('dragOver', 0, $event)"
    @drop="emit('drop', 0, $event)"
    @dragend="emit('dragEnd')"
    @keydown="handleTab"
  >
    <!-- Drag handle — fixed width -->
    <div class="flex w-5 shrink-0 cursor-grab items-center justify-center text-[var(--color-text-dimmed)] active:cursor-grabbing" aria-hidden="true">
      <svg class="h-4 w-4" viewBox="0 0 16 16" fill="currentColor">
        <circle cx="5" cy="4" r="1.2" /><circle cx="11" cy="4" r="1.2" />
        <circle cx="5" cy="8" r="1.2" /><circle cx="11" cy="8" r="1.2" />
        <circle cx="5" cy="12" r="1.2" /><circle cx="11" cy="12" r="1.2" />
      </svg>
    </div>

    <!-- Type toggle — fixed width -->
    <button
      type="button"
      class="ml-1 w-10 shrink-0 rounded px-1 py-1 text-[10px] font-bold uppercase tracking-wide text-center transition"
      :class="combatant.type === 'PC'
        ? 'bg-[var(--color-bg-accent)] text-[var(--color-text-inverted)]'
        : 'bg-[var(--color-bg-hover)] text-[var(--color-text-muted)]'"
      :aria-label="`Type: ${combatant.type}. Click to toggle.`"
      :aria-pressed="combatant.type === 'PC'"
      @click="toggleType"
    >
      {{ combatant.type }}
    </button>

    <!-- Initiative — fixed width -->
    <div class="ml-1.5 flex w-[4.5rem] shrink-0 items-center gap-0.5">
      <input
        type="number"
        :value="combatant.initiative ?? ''"
        data-tab-field
        data-tab-order="1"
        class="w-12 rounded border border-[var(--color-border-primary)] bg-[var(--color-bg-input)] px-1.5 py-1 text-center text-sm text-[var(--color-text-primary)] outline-none focus:border-[var(--color-text-dimmed)] [appearance:textfield] [&::-webkit-inner-spin-button]:appearance-none [&::-webkit-outer-spin-button]:appearance-none"
        aria-label="Initiative"
        placeholder="—"
        @input="handleInitiativeInput"
      />
      <button
        type="button"
        class="rounded p-1 text-[var(--color-text-dimmed)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)] transition"
        aria-label="Roll initiative"
        title="Roll d20"
        @click="emit('roll', combatant.id)"
      >
        <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
          <path d="M10 2l7.5 4.5v7L10 18l-7.5-4.5v-7L10 2zm0 2L4.5 7.5v5L10 16l5.5-3.5v-5L10 4z" />
        </svg>
      </button>
    </div>

    <!-- Name — flexible width -->
    <div class="relative ml-1.5 min-w-0 flex-1">
      <input
        type="text"
        :value="combatant.name"
        data-tab-field
        data-tab-order="2"
        class="w-full rounded border border-[var(--color-border-primary)] bg-[var(--color-bg-input)] px-2 py-1 text-sm text-[var(--color-text-primary)] outline-none focus:border-[var(--color-text-dimmed)]"
        :class="{ 'line-through': isDead }"
        placeholder="Name"
        aria-label="Combatant name"
        @input="handleNameInput"
        @blur="handleNameBlur"
        @keydown.escape="showSuggestions = false"
      />
      <!-- Autocomplete dropdown -->
      <div
        v-if="showSuggestions && suggestions.length > 0"
        class="absolute left-0 top-full z-20 mt-0.5 max-h-32 w-full overflow-y-auto rounded-lg border border-[var(--color-border-primary)] bg-[var(--color-bg-card-solid)] shadow-lg"
        role="listbox"
        aria-label="Character suggestions"
      >
        <button
          v-for="s in suggestions"
          :key="s"
          type="button"
          class="block w-full px-2 py-1 text-left text-sm text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)] transition"
          role="option"
          @mousedown.prevent="selectSuggestion(s)"
        >
          {{ s }}
        </button>
      </div>
    </div>

    <!-- HP — fixed width container to prevent layout shift -->
    <div class="ml-1.5 w-36 shrink-0">
      <HpWidget
        :current-hp="combatant.currentHp"
        :max-hp="combatant.maxHp"
        :combatant-id="combatant.id"
        @adjust="emit('adjustHp', combatant.id, $event)"
        @update:current-hp="emit('update', combatant.id, { currentHp: $event })"
        @update:max-hp="emit('update', combatant.id, { maxHp: $event })"
      />
    </div>

    <!-- AC — fixed width -->
    <div class="ml-1.5 w-12 shrink-0">
      <input
        type="number"
        :value="combatant.ac ?? ''"
        data-tab-field
        data-tab-order="5"
        class="w-full rounded border border-[var(--color-border-primary)] bg-[var(--color-bg-input)] px-1.5 py-1 text-center text-sm text-[var(--color-text-primary)] outline-none focus:border-[var(--color-text-dimmed)] [appearance:textfield] [&::-webkit-inner-spin-button]:appearance-none [&::-webkit-outer-spin-button]:appearance-none"
        aria-label="Armor class"
        placeholder="—"
        @input="handleAcInput"
      />
    </div>

    <!-- Conditions — flexible, hidden in compact mode -->
    <div v-if="!compact" class="ml-1.5 min-w-0 flex-1">
      <input
        type="text"
        :value="combatant.conditions"
        data-tab-field
        data-tab-order="6"
        class="w-full rounded border border-[var(--color-border-primary)] bg-[var(--color-bg-input)] px-2 py-1 text-sm text-[var(--color-text-primary)] outline-none focus:border-[var(--color-text-dimmed)]"
        placeholder="Conditions"
        aria-label="Conditions"
        @input="emit('update', combatant.id, { conditions: ($event.target as HTMLInputElement).value })"
      />
    </div>

    <!-- Dead toggle — fixed width -->
    <button
      type="button"
      class="ml-1 w-7 shrink-0 rounded p-1 transition"
      :class="isDead
        ? 'text-[var(--color-text-danger)] bg-[var(--color-bg-danger)]/20'
        : 'text-[var(--color-text-dimmed)] hover:text-[var(--color-text-danger)] hover:bg-[var(--color-bg-hover)]'"
      :aria-label="isDead ? 'Mark as alive' : 'Mark as dead'"
      :aria-pressed="isDead"
      @click="toggleDead"
    >
      <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
        <path d="M10 2C5.58 2 2 5.58 2 10c0 3.07 1.73 5.73 4.27 7.07L7 15h2v2h2v-2h2l.73 2.07C16.27 15.73 18 13.07 18 10c0-4.42-3.58-8-8-8zM7 11a1.5 1.5 0 110-3 1.5 1.5 0 010 3zm6 0a1.5 1.5 0 110-3 1.5 1.5 0 010 3z" />
      </svg>
    </button>

    <!-- Remove — fixed width, visible on hover -->
    <button
      type="button"
      class="ml-0.5 w-6 shrink-0 rounded p-1 text-[var(--color-text-dimmed)] hover:text-[var(--color-text-danger)] hover:bg-[var(--color-bg-hover)] transition opacity-0 group-hover:opacity-100 focus-visible:opacity-100"
      aria-label="Remove combatant"
      @click="emit('remove', combatant.id)"
    >
      <svg class="h-3.5 w-3.5" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2">
        <path d="M4 4l8 8M12 4l-8 8" />
      </svg>
    </button>
  </div>
</template>
