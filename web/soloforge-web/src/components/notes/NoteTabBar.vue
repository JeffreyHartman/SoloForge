<script setup lang="ts">
defineProps<{
  tabs: string[]
  activeTab: string | null
  sessionLogPath: string
}>()

defineEmits<{
  select: [path: string]
  close: [path: string]
}>()

function displayName(path: string): string {
  const filename = path.split('/').pop() ?? path
  return filename.endsWith('.md') ? filename.slice(0, -3) : filename
}
</script>

<template>
  <div
    v-if="tabs.length > 0"
    class="flex items-end gap-px overflow-x-auto border-b border-[var(--color-border-primary)] bg-[var(--color-bg-card-solid)] px-1"
    role="tablist"
    aria-label="Open notes"
  >
    <div
      v-for="tab in tabs"
      :key="tab"
      class="group/tab flex max-w-[200px] shrink-0 cursor-pointer items-center gap-1.5 rounded-t-lg px-3 py-1.5 text-sm transition-colors"
      :class="tab === activeTab
        ? 'bg-[var(--color-bg-input)] text-[var(--color-text-primary)] border border-b-0 border-[var(--color-border-primary)] -mb-px'
        : 'text-[var(--color-text-muted)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)]'"
      role="tab"
      :aria-selected="tab === activeTab"
      tabindex="0"
      @click="$emit('select', tab)"
      @keydown.enter.prevent="$emit('select', tab)"
    >
      <span class="truncate">{{ displayName(tab) }}</span>
      <span
        v-if="tab === sessionLogPath"
        class="shrink-0 rounded px-1 py-0.5 text-[8px] font-bold uppercase tracking-wider"
        :class="tab === activeTab
          ? 'bg-[var(--color-bg-info)] text-[var(--color-text-info)]'
          : 'bg-[var(--color-bg-muted)] text-[var(--color-text-dimmed)]'"
      >
        Log
      </span>
      <button
        class="shrink-0 rounded p-0.5 transition"
        :class="tab === activeTab
          ? 'text-[var(--color-text-dimmed)] hover:text-[var(--color-text-danger)] hover:bg-[var(--color-bg-danger)]'
          : 'text-transparent group-hover/tab:text-[var(--color-text-dimmed)] hover:!text-[var(--color-text-danger)] hover:!bg-[var(--color-bg-danger)]'"
        :title="`Close ${displayName(tab)}`"
        :aria-label="`Close ${displayName(tab)}`"
        @click.stop="$emit('close', tab)"
      >
        <svg class="h-3 w-3" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2.5">
          <path d="M4 4l8 8M12 4l-8 8" />
        </svg>
      </button>
    </div>
  </div>
</template>
