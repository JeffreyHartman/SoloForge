<script setup lang="ts">
import type { FateCheckResponse } from '../../types'
import { ODDS_OPTIONS } from '../../types'
import BaseCard from '../common/BaseCard.vue'
import BaseButton from '../common/BaseButton.vue'
import BaseInput from '../common/BaseInput.vue'
import BaseSelect from '../common/BaseSelect.vue'

defineProps<{
  chaos: number
  result: FateCheckResponse | null
  loading: boolean
  apiOnline: boolean
}>()

const odds = defineModel<string>('odds')
const question = defineModel<string>('question')

defineEmits<{
  roll: []
}>()
</script>

<template>
  <BaseCard title="Fate Check">
    <template #header>
      <div class="text-xs text-[var(--color-text-dimmed)]">Chaos {{ chaos }}</div>
    </template>

    <div class="grid grid-cols-1 gap-3">
      <BaseSelect v-model="odds" label="Odds" :options="ODDS_OPTIONS" />

      <BaseInput
        v-model="question"
        label="Question (optional)"
        placeholder="Does the guard notice me?"
        @enter="$emit('roll')"
      />

      <BaseButton
        :disabled="loading || !apiOnline"
        :loading="loading"
        @click="$emit('roll')"
      >
        Roll
      </BaseButton>
    </div>

    <div v-if="result" class="mt-4 rounded-2xl border border-[var(--color-border-card)] bg-[var(--color-bg-card-solid)] p-4">
      <div class="flex items-start justify-between gap-3">
        <div>
          <div class="text-xs font-medium text-[var(--color-text-dimmed)]">Result</div>
          <div
            class="mt-1 text-lg font-semibold"
            :class="
              result.fate.result.includes('Yes')
                ? 'text-[var(--color-text-success)]'
                : result.fate.result.includes('No')
                  ? 'text-[var(--color-text-danger)]'
                  : 'text-[var(--color-text-primary)]'
            "
          >
            {{ result.fate.result }}
          </div>
          <div class="mt-1 text-xs text-[var(--color-text-muted)]">
            Roll {{ result.fate.roll }} · Odds {{ result.odds }}
          </div>
        </div>
        <div class="rounded-xl bg-[var(--color-bg-muted)] px-3 py-2 text-center">
          <div class="text-[11px] font-medium text-[var(--color-text-dimmed)]">Chaos</div>
          <div class="mt-0.5 text-base font-semibold tabular-nums text-[var(--color-text-primary)]">{{ result.chaos }}</div>
        </div>
      </div>

      <div v-if="result.randomEvent" class="mt-4 rounded-xl border border-[var(--color-border-warning)] bg-[var(--color-bg-warning-subtle)] p-3">
        <div class="text-xs font-semibold text-[var(--color-text-warning)]">Random event</div>
        <div class="mt-1 text-sm font-semibold text-[var(--color-text-primary)]">
          {{ result.randomEvent.eventFocus }}: {{ result.randomEvent.eventAction }}
        </div>
        <div v-if="result.randomEvent.selectedCharacter" class="mt-1 text-xs text-[var(--color-text-secondary)]">
          Character: {{ result.randomEvent.selectedCharacter }}
        </div>
        <div v-if="result.randomEvent.selectedThread" class="mt-1 text-xs text-[var(--color-text-secondary)]">
          Thread: {{ result.randomEvent.selectedThread }}
        </div>
        <div v-if="result.randomEvent.listWasEmpty" class="mt-1 text-xs text-[var(--color-text-secondary)]">
          (List was empty)
        </div>
      </div>
    </div>
  </BaseCard>
</template>
