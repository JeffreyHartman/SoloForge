<script setup lang="ts">
import { ref, watch, nextTick } from 'vue'

const props = defineProps<{
  round: number
  started: boolean
}>()

const emit = defineEmits<{
  next: []
  prev: []
  'update:round': [value: number]
}>()

const editing = ref(false)
const editValue = ref(props.round)
const editInput = ref<HTMLInputElement | null>(null)
const canceling = ref(false)

watch(() => props.round, (val) => {
  editValue.value = val
})

function startEdit() {
  editValue.value = props.round
  editing.value = true
  nextTick(() => {
    editInput.value?.select()
  })
}

function commitEdit() {
  if (canceling.value) return
  const num = Number(editValue.value)
  const val = Number.isFinite(num) ? Math.max(1, Math.floor(num)) : props.round
  emit('update:round', val)
  editing.value = false
}

function cancelEdit() {
  canceling.value = true
  editing.value = false
  nextTick(() => { canceling.value = false })
}
</script>

<template>
  <div class="flex items-center gap-2">
    <button
      type="button"
      class="inline-flex items-center gap-1 rounded-lg border border-[var(--color-border-primary)] px-3 py-1.5 text-sm font-medium text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)] transition disabled:opacity-40"
      :disabled="!started"
      aria-label="Previous turn"
      @click="emit('prev')"
    >
      <svg class="h-3.5 w-3.5" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
        <path d="M10 4l-4 4 4 4" />
      </svg>
      Back
    </button>

    <!-- Round display / click-to-edit -->
    <div class="min-w-[3.5rem] text-center">
      <span class="block text-[10px] uppercase tracking-wider text-[var(--color-text-muted)]">Round</span>
      <button
        v-if="!editing"
        type="button"
        class="text-lg font-bold leading-tight text-[var(--color-text-primary)] hover:text-[var(--color-text-accent)] transition cursor-pointer"
        aria-label="Edit round number"
        @click="startEdit"
      >
        {{ round }}
      </button>
      <input
        v-else
        ref="editInput"
        v-model.number="editValue"
        type="number"
        min="1"
        class="w-14 rounded border border-[var(--color-border-primary)] bg-[var(--color-bg-input)] px-1.5 py-0.5 text-center text-sm font-bold text-[var(--color-text-primary)] outline-none focus:border-[var(--color-text-dimmed)] [appearance:textfield] [&::-webkit-inner-spin-button]:appearance-none [&::-webkit-outer-spin-button]:appearance-none"
        aria-label="Round number"
        @keydown.enter="commitEdit"
        @keydown.escape="cancelEdit"
        @blur="commitEdit"
      />
    </div>

    <button
      type="button"
      class="inline-flex items-center gap-1 rounded-lg bg-[var(--color-bg-accent)] px-3 py-1.5 text-sm font-medium text-[var(--color-text-inverted)] hover:bg-[var(--color-bg-accent-hover)] transition"
      aria-label="Next turn"
      @click="emit('next')"
    >
      Next
      <svg class="h-3.5 w-3.5" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2">
        <path d="M6 4l4 4-4 4" />
      </svg>
    </button>
  </div>
</template>
