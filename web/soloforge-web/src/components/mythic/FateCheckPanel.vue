<script setup lang="ts">
import { ref } from 'vue'
import { ODDS_OPTIONS } from '../../types'
import BaseCard from '../common/BaseCard.vue'
import BaseButton from '../common/BaseButton.vue'
import BaseInput from '../common/BaseInput.vue'
import BaseSelect from '../common/BaseSelect.vue'
import { useMythic } from '../../composables/useMythic'
import { useCampaign } from '../../composables/useCampaign'
import { useToolActions } from '../../composables/useToolActions'
import { useResultBanner, formatFateResult } from '../../composables/useResultBanner'
import { fateCheckToMarkdown, copyToClipboard } from '../../composables/useRollMarkdown'

const props = withDefaults(defineProps<{ mode?: 'full' | 'toolbar' }>(), { mode: 'full' })

const emit = defineEmits<{ rolled: [] }>()

const { fateOdds, fateQuestion, fateResult, runFateCheck, loading } = useMythic()
const { session } = useCampaign()
const { apiOnline, runAction } = useToolActions()
const { showBanner } = useResultBanner()

const copied = ref(false)

async function handleRoll() {
  await runAction(() => runFateCheck())
  if (fateResult.value && props.mode === 'toolbar') {
    showBanner(formatFateResult(fateResult.value))
    emit('rolled')
  }
}

async function handleCopy() {
  if (!fateResult.value) return
  const success = await copyToClipboard(fateCheckToMarkdown(fateResult.value))
  if (success) {
    copied.value = true
    setTimeout(() => { copied.value = false }, 1500)
  }
}
</script>

<template>
  <BaseCard v-if="mode === 'full'" title="Fate Check">
    <template #header>
      <div class="text-xs text-[var(--color-text-dimmed)]">Chaos {{ session.chaos }}</div>
    </template>
    <template v-if="$slots.pin" #pin><slot name="pin" /></template>

    <div class="grid grid-cols-1 gap-3">
      <BaseSelect v-model="fateOdds" label="Odds" :options="ODDS_OPTIONS" />

      <BaseInput
        v-model="fateQuestion"
        label="Question (optional)"
        placeholder="Does the guard notice me?"
        @enter="handleRoll"
      />

      <BaseButton
        :disabled="loading.fateCheck || !apiOnline"
        :loading="loading.fateCheck"
        @click="handleRoll"
      >
        Roll
      </BaseButton>
    </div>

    <div v-if="fateResult" class="group/result mt-4 rounded-2xl border border-[var(--color-border-card)] bg-[var(--color-bg-card-solid)] p-4">
      <button
        type="button"
        class="float-right ml-2 rounded-lg p-1.5 text-[var(--color-text-dimmed)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)] transition opacity-0 group-hover/result:opacity-100 focus:opacity-100"
        :title="copied ? 'Copied!' : 'Copy as markdown'"
        :aria-label="copied ? 'Copied!' : 'Copy as markdown'"
        @click="handleCopy"
      >
        <svg v-if="!copied" class="h-4 w-4" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5">
          <rect x="5" y="5" width="9" height="9" rx="1.5" />
          <path d="M11 5V3.5A1.5 1.5 0 009.5 2h-6A1.5 1.5 0 002 3.5v6A1.5 1.5 0 003.5 11H5" />
        </svg>
        <svg v-else class="h-4 w-4 text-[var(--color-text-success)]" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2">
          <path d="M3 8l3 3 7-7" />
        </svg>
      </button>
      <div class="flex items-start justify-between gap-3">
        <div>
          <div class="text-xs font-medium text-[var(--color-text-dimmed)]">Result</div>
          <div
            class="mt-1 text-lg font-semibold"
            :class="
              fateResult.fate.result.includes('Yes')
                ? 'text-[var(--color-text-success)]'
                : fateResult.fate.result.includes('No')
                  ? 'text-[var(--color-text-danger)]'
                  : 'text-[var(--color-text-primary)]'
            "
          >
            {{ fateResult.fate.result }}
          </div>
          <div class="mt-1 text-xs text-[var(--color-text-muted)]">
            Roll {{ fateResult.fate.roll }} · Odds {{ fateResult.odds }}
          </div>
        </div>
        <div class="rounded-xl bg-[var(--color-bg-muted)] px-3 py-2 text-center">
          <div class="text-[11px] font-medium text-[var(--color-text-dimmed)]">Chaos</div>
          <div class="mt-0.5 text-base font-semibold tabular-nums text-[var(--color-text-primary)]">{{ fateResult.chaos }}</div>
        </div>
      </div>

      <div v-if="fateResult.randomEvent" class="mt-4 rounded-xl border border-[var(--color-border-warning)] bg-[var(--color-bg-warning-subtle)] p-3">
        <div class="text-xs font-semibold text-[var(--color-text-warning)]">Random event</div>
        <div class="mt-1 text-sm font-semibold text-[var(--color-text-primary)]">
          {{ fateResult.randomEvent.eventFocus }}: {{ fateResult.randomEvent.eventAction }}
        </div>
        <div v-if="fateResult.randomEvent.selectedCharacter" class="mt-1 text-xs text-[var(--color-text-secondary)]">
          Character: {{ fateResult.randomEvent.selectedCharacter }}
        </div>
        <div v-if="fateResult.randomEvent.selectedThread" class="mt-1 text-xs text-[var(--color-text-secondary)]">
          Thread: {{ fateResult.randomEvent.selectedThread }}
        </div>
        <div v-if="fateResult.randomEvent.listWasEmpty" class="mt-1 text-xs text-[var(--color-text-secondary)]">
          (List was empty)
        </div>
      </div>
    </div>
  </BaseCard>

  <!-- Compact toolbar mode -->
  <div v-else class="space-y-3">
    <div class="grid grid-cols-1 gap-3">
      <BaseSelect v-model="fateOdds" label="Odds" :options="ODDS_OPTIONS" />

      <BaseInput
        v-model="fateQuestion"
        label="Question (optional)"
        placeholder="Does the guard notice me?"
        @enter="handleRoll"
      />

      <BaseButton
        :disabled="loading.fateCheck || !apiOnline"
        :loading="loading.fateCheck"
        @click="handleRoll"
      >
        Roll
      </BaseButton>
    </div>
  </div>
</template>
