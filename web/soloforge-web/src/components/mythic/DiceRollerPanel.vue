<script setup lang="ts">
import type { DiceRollResponse } from '../../types'
import { QUICK_DICE } from '../../types'
import BaseCard from '../common/BaseCard.vue'
import BaseButton from '../common/BaseButton.vue'
import BaseInput from '../common/BaseInput.vue'

defineProps<{
  result: DiceRollResponse | null
  loading: boolean
  apiOnline: boolean
}>()

const expression = defineModel<string>('expression')

defineEmits<{
  roll: [expr?: string]
}>()
</script>

<template>
  <BaseCard title="Dice Roller">
    <template #header>
      <div class="text-xs text-[var(--color-text-dimmed)]">2d6+1, d20, 1d8-2</div>
    </template>

    <div class="grid grid-cols-1 gap-3">
      <BaseInput
        v-model="expression"
        label="Expression"
        placeholder="2d6+1"
        @enter="$emit('roll', expression)"
      />

      <div class="flex flex-wrap gap-2">
        <button
          v-for="die in QUICK_DICE"
          :key="die"
          type="button"
          class="rounded-full border border-[var(--color-border-primary)] bg-[var(--color-bg-card-solid)] px-3 py-1 text-xs font-semibold text-[var(--color-text-primary)] shadow-sm transition hover:bg-[var(--color-bg-hover)]"
          @click="$emit('roll', '1' + die)"
        >
          {{ die }}
        </button>
      </div>

      <BaseButton
        :disabled="loading || !apiOnline"
        :loading="loading"
        @click="$emit('roll', expression)"
      >
        Roll
      </BaseButton>
    </div>

    <div v-if="result" class="mt-4 rounded-2xl border border-[var(--color-border-card)] bg-[var(--color-bg-card-solid)] p-4">
      <div class="text-xs font-medium text-[var(--color-text-dimmed)]">Result</div>
      <div class="mt-1 text-lg font-semibold text-[var(--color-text-primary)]">{{ result.roll.summary }}</div>
      <div v-if="result.breakdown" class="mt-2 rounded-xl bg-[var(--color-bg-muted)] px-3 py-2 font-mono text-[12px] leading-5 text-[var(--color-text-secondary)]">
        {{ result.breakdown }}
      </div>
    </div>
  </BaseCard>
</template>
