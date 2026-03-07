<script setup lang="ts">
import { useTheme } from '../../composables'

defineProps<{
  campaignName: string
  chaos: number
  apiOnline: boolean | null
  isBusy: boolean
}>()

defineEmits<{
  refresh: []
}>()

const { isDark, toggleTheme } = useTheme()
</script>

<template>
  <header class="relative mx-auto max-w-6xl px-4 pt-6 sm:pt-10">
    <div class="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
      <div>
        <div class="inline-flex items-center gap-3">
          <div class="h-10 w-10 rounded-2xl bg-[var(--color-bg-accent)] text-[var(--color-text-inverted)] shadow-sm ring-1 ring-white/20 dark:ring-black/20">
            <div class="grid h-full w-full place-items-center text-sm font-semibold">SF</div>
          </div>
          <div>
            <h1 class="text-2xl font-semibold tracking-tight text-[var(--color-text-primary)]">SoloForge</h1>
            <p class="text-sm text-[var(--color-text-muted)]">{{ campaignName }}</p>
          </div>
        </div>
      </div>

      <div class="flex items-center gap-2">
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

        <button
          type="button"
          class="rounded-full border border-[var(--color-border-secondary)] bg-[var(--color-bg-card)] p-2 text-[var(--color-text-secondary)] shadow-sm backdrop-blur transition hover:bg-[var(--color-bg-card-solid)]"
          title="Toggle theme"
          @click="toggleTheme"
        >
          <svg v-if="isDark" class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 3v1m0 16v1m9-9h-1M4 12H3m15.364 6.364l-.707-.707M6.343 6.343l-.707-.707m12.728 0l-.707.707M6.343 17.657l-.707.707M16 12a4 4 0 11-8 0 4 4 0 018 0z" />
          </svg>
          <svg v-else class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20.354 15.354A9 9 0 018.646 3.646 9.003 9.003 0 0012 21a9.003 9.003 0 008.354-5.646z" />
          </svg>
        </button>

        <button
          class="rounded-full border border-[var(--color-border-secondary)] bg-[var(--color-bg-card)] px-4 py-1.5 text-xs font-medium text-[var(--color-text-primary)] shadow-sm backdrop-blur transition hover:bg-[var(--color-bg-card-solid)]"
          type="button"
          :disabled="isBusy"
          @click="$emit('refresh')"
        >
          Refresh
        </button>
      </div>
    </div>
  </header>
</template>
