<script setup lang="ts">
const props = withDefaults(defineProps<{
  open: boolean
  title: string
  expandedWidth?: number
  collapsedWidth?: number
}>(), {
  expandedWidth: 280,
  collapsedWidth: 36,
})

defineEmits<{
  'update:open': [value: boolean]
}>()
</script>

<template>
  <div
    class="shrink-0 overflow-hidden transition-all duration-300 ease-in-out"
    :style="{ width: open ? `${props.expandedWidth}px` : `${props.collapsedWidth}px` }"
  >
    <!-- Collapsed state: narrow strip with expand chevron -->
    <div
      v-if="!open"
      class="flex h-full flex-col items-center border-r border-[var(--color-border-primary)] bg-[var(--color-bg-card-solid)] pt-2"
      :style="{ width: `${props.collapsedWidth}px` }"
    >
      <button
        class="rounded-lg p-1.5 text-[var(--color-text-dimmed)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)] transition"
        :aria-expanded="false"
        :aria-label="`Open ${title} sidebar`"
        @click="$emit('update:open', true)"
      >
        <svg class="h-4 w-4" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2">
          <path d="M6 4l4 4-4 4" />
        </svg>
      </button>
    </div>

    <!-- Expanded state: header + content -->
    <div
      v-else
      class="flex h-full flex-col border-r border-[var(--color-border-primary)] bg-[var(--color-bg-card-solid)]"
      :style="{ width: `${props.expandedWidth}px` }"
    >
      <!-- Header -->
      <div class="flex items-center justify-between border-b border-[var(--color-border-primary)] px-3 py-2.5">
        <span class="text-xs font-semibold uppercase tracking-wider text-[var(--color-text-muted)]">{{ title }}</span>
        <div class="flex items-center gap-1">
          <slot name="header-actions" />
          <button
            class="rounded p-1 text-[var(--color-text-dimmed)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)] transition"
            title="Collapse sidebar"
            :aria-label="`Collapse ${title} sidebar`"
            :aria-expanded="true"
            @click="$emit('update:open', false)"
          >
            <svg class="h-4 w-4" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M10 4l-4 4 4 4" />
            </svg>
          </button>
        </div>
      </div>

      <!-- Content area (consumer manages scroll) -->
      <div class="flex min-h-0 flex-1 flex-col">
        <slot />
      </div>
    </div>
  </div>
</template>
