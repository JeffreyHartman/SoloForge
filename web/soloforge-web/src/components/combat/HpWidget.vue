<script setup lang="ts">
import { ref, watch, nextTick } from 'vue'

const props = defineProps<{
  currentHp: number
  maxHp: number
  combatantId: string
}>()

const emit = defineEmits<{
  adjust: [delta: number]
  'update:currentHp': [value: number]
  'update:maxHp': [value: number]
}>()

const editingCurrent = ref(false)
const editingMax = ref(false)
const editValue = ref('')
const currentInput = ref<HTMLInputElement | null>(null)
const maxInput = ref<HTMLInputElement | null>(null)

// Sync editValue when props change (e.g. from +/- button adjustments)
watch(() => props.currentHp, (val) => {
  if (editingCurrent.value) editValue.value = String(val)
})
watch(() => props.maxHp, (val) => {
  if (editingMax.value) editValue.value = String(val)
})

function startEditCurrent() {
  editValue.value = String(props.currentHp)
  editingCurrent.value = true
  nextTick(() => {
    const el = currentInput.value
    if (el) {
      el.focus()
      // Place cursor at end instead of selecting all
      el.setSelectionRange(el.value.length, el.value.length)
    }
  })
}

function startEditMax() {
  editValue.value = String(props.maxHp)
  editingMax.value = true
  nextTick(() => {
    const el = maxInput.value
    if (el) {
      el.focus()
      el.setSelectionRange(el.value.length, el.value.length)
    }
  })
}

function evaluateExpression(raw: string, base: number): number | null {
  const trimmed = raw.trim()
  if (!trimmed) return null

  // Allow expressions like "10-2", "10+5", "-3", "+4", or plain numbers
  // If input starts with +/-, treat as relative to base
  if (/^[+-]\d+$/.test(trimmed)) {
    return base + Number(trimmed)
  }

  // Full expression: "10-2", "15+3"
  const match = trimmed.match(/^(-?\d+)\s*([+-])\s*(\d+)$/)
  if (match) {
    const left = Number(match[1])
    const op = match[2]
    const right = Number(match[3])
    return op === '+' ? left + right : left - right
  }

  // Plain number
  const num = Number(trimmed)
  if (!isNaN(num) && /^-?\d+$/.test(trimmed)) return num

  return null
}

function filterInput(e: InputEvent) {
  const input = e.target as HTMLInputElement
  // Only allow digits, +, -, and spaces
  input.value = input.value.replace(/[^0-9+\-\s]/g, '')
  editValue.value = input.value
}

function commitCurrent() {
  const result = evaluateExpression(editValue.value, props.currentHp)
  if (result !== null) {
    emit('update:currentHp', result)
  }
  editingCurrent.value = false
}

function commitMax() {
  const result = evaluateExpression(editValue.value, props.maxHp)
  if (result !== null) {
    emit('update:maxHp', result)
  }
  editingMax.value = false
}

function cancelEdit() {
  editingCurrent.value = false
  editingMax.value = false
}

function handleBlur() {
  // Delay check to allow v-if swap (button→input) to complete
  setTimeout(() => {
    const active = document.activeElement
    const container = document.querySelector(`[data-hp-widget="${props.combatantId}"]`)
    if (container?.contains(active)) return
    if (editingCurrent.value) commitCurrent()
    if (editingMax.value) commitMax()
  }, 0)
}
</script>

<template>
  <div
    class="inline-flex items-center gap-0.5"
    :data-hp-widget="combatantId"
    @focusout="handleBlur"
  >
    <!-- Current HP: plain text or edit input with floating +/- popover -->
    <div class="relative">
      <button
        v-if="!editingCurrent"
        type="button"
        data-tab-field
        data-tab-order="3"
        class="w-14 rounded px-1 py-1 text-center text-sm font-semibold text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)] transition cursor-text"
        aria-label="Current HP for combatant"
        @click="startEditCurrent"
      >
        {{ currentHp }}
      </button>
      <template v-else>
        <input
          ref="currentInput"
          :value="editValue"
          type="text"
          inputmode="numeric"
          data-tab-field
          data-tab-order="3"
          class="w-14 rounded border border-[var(--color-border-primary)] bg-[var(--color-bg-input)] px-1.5 py-1 text-center text-sm font-semibold text-[var(--color-text-primary)] outline-none focus:border-[var(--color-text-dimmed)] ring-1 ring-[var(--color-text-accent)]"
          aria-label="Current HP for combatant"
          @input="filterInput($event as InputEvent)"
          @keydown.enter="commitCurrent"
          @keydown.escape="cancelEdit"
        />
        <!-- Floating +/- popover above the input -->
        <div class="absolute bottom-full left-1/2 z-30 mb-1 flex -translate-x-1/2 items-center gap-0.5 rounded-lg border border-[var(--color-border-primary)] bg-[var(--color-bg-card-solid)] px-1 py-0.5 shadow-lg">
          <button
            type="button"
            class="flex h-6 w-6 items-center justify-center rounded text-sm font-bold text-[var(--color-text-danger)] hover:bg-[var(--color-bg-hover)] transition"
            aria-label="Reduce HP"
            tabindex="-1"
            @mousedown.prevent="emit('adjust', -1)"
          >
            &minus;
          </button>
          <button
            type="button"
            class="flex h-6 w-6 items-center justify-center rounded text-sm font-bold text-[var(--color-text-success)] hover:bg-[var(--color-bg-hover)] transition"
            aria-label="Increase HP"
            tabindex="-1"
            @mousedown.prevent="emit('adjust', 1)"
          >
            +
          </button>
        </div>
      </template>
    </div>

    <span class="text-sm text-[var(--color-text-muted)]">/</span>

    <!-- Max HP: plain text or edit input -->
    <button
      v-if="!editingMax"
      type="button"
      data-tab-field
      data-tab-order="4"
      class="w-14 rounded px-1 py-1 text-center text-sm text-[var(--color-text-muted)] hover:bg-[var(--color-bg-hover)] transition cursor-text"
      aria-label="Max HP for combatant"
      @click="startEditMax"
    >
      {{ maxHp }}
    </button>
    <input
      v-else
      ref="maxInput"
      :value="editValue"
      type="text"
      inputmode="numeric"
      data-tab-field
      data-tab-order="4"
      class="w-14 rounded border border-[var(--color-border-primary)] bg-[var(--color-bg-input)] px-1.5 py-1 text-center text-sm text-[var(--color-text-primary)] outline-none focus:border-[var(--color-text-dimmed)] ring-1 ring-[var(--color-text-accent)]"
      aria-label="Max HP for combatant"
      @input="filterInput($event as InputEvent)"
      @keydown.enter="commitMax"
      @keydown.escape="cancelEdit"
    />
  </div>
</template>
