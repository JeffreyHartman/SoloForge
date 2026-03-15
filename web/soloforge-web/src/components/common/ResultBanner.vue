<script setup lang="ts">
import { computed } from 'vue'
import { useResultBanner } from '../../composables/useResultBanner'
import type { ResultType } from '../../composables/useResultBanner'

const { banner, dismissBanner } = useResultBanner()

interface BannerStyle {
  border: string
  color: string
  bg: string
  label: string
}

const STYLES: Record<ResultType, BannerStyle> = {
  fate:    { border: 'var(--color-roll-fate)',    color: 'var(--color-roll-fate)',         bg: 'var(--color-roll-fate-bg)',    label: 'Fate' },
  scene:   { border: 'var(--color-roll-scene)',   color: 'var(--color-roll-scene-text)',   bg: 'var(--color-roll-scene-bg)',   label: 'Scene' },
  event:   { border: 'var(--color-roll-event)',   color: 'var(--color-roll-event-text)',   bg: 'var(--color-roll-event-bg)',   label: 'Event' },
  meaning: { border: 'var(--color-roll-meaning)', color: 'var(--color-roll-meaning-text)', bg: 'var(--color-roll-meaning-bg)', label: 'Meaning' },
  dice:    { border: 'var(--color-roll-dice)',    color: 'var(--color-roll-dice-text)',    bg: 'var(--color-roll-dice-bg)',    label: 'Dice' },
}

const style = computed(() => banner.value ? STYLES[banner.value.type] : STYLES.fate)

const isFateYes = computed(() =>
  banner.value?.type === 'fate' && banner.value.title.includes('Yes')
)
const isFateNo = computed(() =>
  banner.value?.type === 'fate' && banner.value.title.includes('No')
)
</script>

<template>
  <Transition
    enter-active-class="transition duration-200 ease-out"
    enter-from-class="opacity-0 scale-95"
    enter-to-class="opacity-100 scale-100"
    leave-active-class="transition duration-150 ease-in"
    leave-from-class="opacity-100 scale-100"
    leave-to-class="opacity-0 scale-95"
  >
    <div
      v-if="banner"
      role="status"
      aria-live="polite"
      class="fixed top-24 left-1/2 z-50 w-full max-w-md -translate-x-1/2 rounded-2xl border border-[var(--color-border-primary)] bg-[var(--color-bg-card-solid)] px-5 py-4 shadow-xl"
      :style="{ borderLeft: `4px solid ${style.border}` }"
    >
      <div class="flex items-start gap-3">
        <div class="min-w-0 flex-1">
          <div class="mb-1 flex items-center gap-2">
            <span
              class="shrink-0 rounded-md px-2 py-0.5 text-[10px] font-bold uppercase tracking-wider"
              :style="{ color: style.color, backgroundColor: style.bg }"
            >
              {{ style.label }}
            </span>
          </div>
          <div
            class="text-lg font-semibold"
            :class="
              isFateYes
                ? 'text-[var(--color-text-success)]'
                : isFateNo
                  ? 'text-[var(--color-text-danger)]'
                  : 'text-[var(--color-text-primary)]'
            "
          >
            {{ banner.title }}
          </div>
          <div v-if="banner.detail" class="mt-1 text-sm text-[var(--color-text-muted)]">
            {{ banner.detail }}
          </div>
          <div v-if="banner.subDetail" class="mt-1 text-xs text-[var(--color-text-dimmed)]">
            {{ banner.subDetail }}
          </div>
        </div>
        <button
          type="button"
          class="shrink-0 rounded-lg p-1 text-[var(--color-text-dimmed)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)] transition"
          aria-label="Dismiss result"
          @click="dismissBanner"
        >
          <svg class="h-4 w-4" viewBox="0 0 14 14" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M2 2l10 10M12 2L2 12" />
          </svg>
        </button>
      </div>
    </div>
  </Transition>
</template>
