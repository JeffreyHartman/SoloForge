<script setup lang="ts">
import ThemePicker from './ThemePicker.vue'
import LastEvent from './LastEvent.vue'

defineProps<{
  campaignName: string
  chaos: number
  apiOnline: boolean | null
  isBusy: boolean
}>()

defineEmits<{
  refresh: []
}>()
</script>

<template>
  <header class="relative mx-auto max-w-6xl px-4 pt-6 sm:pt-10">
    <div class="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
      <div>
        <div class="inline-flex items-center gap-3">
          <div class="h-10 w-10 rounded-2xl bg-[var(--color-bg-accent)] text-[var(--color-text-inverted)] shadow-sm ring-1 ring-[var(--color-border-secondary)]">
            <div class="grid h-full w-full place-items-center text-sm font-semibold">SF</div>
          </div>
          <div>
            <h1 class="text-2xl font-semibold tracking-tight text-[var(--color-text-primary)]">SoloForge</h1>
            <p class="text-sm text-[var(--color-text-muted)]">{{ campaignName }}</p>
          </div>
        </div>
      </div>

      <div class="flex items-center gap-2">
        <LastEvent />

        <div class="rounded-2xl border border-[var(--color-border-card)] bg-[var(--color-bg-card-solid)] px-3 py-2 text-center shadow-sm">
          <div class="text-[11px] font-medium text-[var(--color-text-dimmed)]">Chaos</div>
          <div class="mt-0.5 text-lg font-semibold tabular-nums text-[var(--color-text-primary)]">{{ chaos }}</div>
        </div>

        <div
          class="inline-flex items-center gap-2 rounded-full border border-[var(--color-border-secondary)] bg-[var(--color-bg-card)] px-3 py-1.5 text-xs text-[var(--color-text-secondary)] shadow-sm backdrop-blur"
        >
          <span
            class="h-2 w-2 rounded-full"
            :class="
              apiOnline === null
                ? 'bg-[var(--color-status-pending)]'
                : apiOnline
                  ? 'bg-[var(--color-status-online)]'
                  : 'bg-[var(--color-status-offline)]'
            "
          />
          <span v-if="apiOnline === null">Checking...</span>
          <span v-else-if="apiOnline">Online</span>
          <span v-else>Offline</span>
        </div>

        <ThemePicker />

        <button
          class="rounded-full border border-[var(--color-border-secondary)] bg-[var(--color-bg-card)] px-4 py-1.5 text-xs font-medium text-[var(--color-text-primary)] shadow-sm backdrop-blur transition"
          :class="isBusy ? 'opacity-50 cursor-not-allowed' : 'hover:bg-[var(--color-bg-card-solid)]'"
          type="button"
          :disabled="isBusy"
          :aria-busy="isBusy"
          @click="$emit('refresh')"
        >
          Refresh
        </button>
      </div>
    </div>
  </header>
</template>
