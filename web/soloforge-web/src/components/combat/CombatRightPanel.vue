<script setup lang="ts">
import { ref } from 'vue'
import CombatTrackerPanel from './CombatTrackerPanel.vue'

const props = defineProps<{
  width: number
}>()

const emit = defineEmits<{
  'update:width': [value: number]
  close: []
}>()

const MIN_WIDTH = 280
const MAX_WIDTH = 600

const resizing = ref(false)

function startResize(e: MouseEvent) {
  e.preventDefault()
  resizing.value = true
  const startX = e.clientX
  const startWidth = props.width

  function onMove(ev: MouseEvent) {
    const delta = startX - ev.clientX
    const newWidth = Math.min(MAX_WIDTH, Math.max(MIN_WIDTH, startWidth + delta))
    emit('update:width', newWidth)
  }

  function onUp() {
    resizing.value = false
    document.removeEventListener('mousemove', onMove)
    document.removeEventListener('mouseup', onUp)
  }

  document.addEventListener('mousemove', onMove)
  document.addEventListener('mouseup', onUp)
}
</script>

<template>
  <div
    class="relative flex shrink-0 border-l border-[var(--color-border-primary)]"
    :style="{ width: `${width}px` }"
    aria-label="Combat tracker panel"
    role="complementary"
  >
    <!-- Resize handle -->
    <div
      class="absolute -left-1 top-0 z-10 h-full w-2 cursor-col-resize hover:bg-[var(--color-bg-accent)]/20 transition"
      :class="{ 'bg-[var(--color-bg-accent)]/30': resizing }"
      aria-label="Resize combat panel"
      role="separator"
      @mousedown="startResize"
    />

    <div class="flex h-full w-full flex-col overflow-hidden bg-[var(--color-bg-card-solid)]">
      <!-- Header -->
      <div class="flex items-center justify-between border-b border-[var(--color-border-primary)] px-3 py-2">
        <span class="text-xs font-semibold uppercase tracking-wider text-[var(--color-text-muted)]">Combat</span>
        <button
          type="button"
          class="rounded p-1 text-[var(--color-text-dimmed)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)] transition"
          aria-label="Close combat panel"
          @click="emit('close')"
        >
          <svg class="h-4 w-4" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M4 4l8 8M12 4l-8 8" />
          </svg>
        </button>
      </div>

      <!-- Tracker content -->
      <div class="min-h-0 flex-1">
        <CombatTrackerPanel compact />
      </div>
    </div>
  </div>
</template>
