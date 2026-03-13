<script setup lang="ts">
import { ref } from 'vue'
import { QUICK_DICE } from '../../types'
import BaseCard from '../common/BaseCard.vue'
import BaseButton from '../common/BaseButton.vue'
import BaseInput from '../common/BaseInput.vue'
import { useMythic } from '../../composables/useMythic'
import { useToolActions } from '../../composables/useToolActions'
import { diceRollToMarkdown, copyToClipboard } from '../../composables/useRollMarkdown'

const { diceExpression, diceResult, rollDice, loading } = useMythic()
const { apiOnline, runAction } = useToolActions()

const copied = ref(false)

function handleRoll(expr?: string) {
  void runAction(() => rollDice(expr))
}

async function handleCopy() {
  if (!diceResult.value) return
  const success = await copyToClipboard(diceRollToMarkdown(diceResult.value))
  if (success) {
    copied.value = true
    setTimeout(() => { copied.value = false }, 1500)
  }
}
</script>

<template>
  <BaseCard title="Dice Roller">
    <template #header>
      <div class="text-xs text-[var(--color-text-dimmed)]">2d6+1, d20, 1d8-2</div>
    </template>

    <div class="grid grid-cols-1 gap-3">
      <BaseInput
        v-model="diceExpression"
        label="Expression"
        placeholder="2d6+1"
        @enter="handleRoll(diceExpression)"
      />

      <div class="flex flex-wrap gap-2">
        <button
          v-for="die in QUICK_DICE"
          :key="die"
          type="button"
          class="rounded-full border border-[var(--color-border-primary)] bg-[var(--color-bg-card-solid)] px-3 py-1 text-xs font-semibold text-[var(--color-text-primary)] shadow-sm transition hover:bg-[var(--color-bg-hover)]"
          :aria-label="`Roll 1${die}`"
          @click="handleRoll('1' + die)"
        >
          {{ die }}
        </button>
      </div>

      <BaseButton
        :disabled="loading.diceRoll || !apiOnline"
        :loading="loading.diceRoll"
        @click="handleRoll(diceExpression)"
      >
        Roll
      </BaseButton>
    </div>

    <div v-if="diceResult" class="group/result mt-4 rounded-2xl border border-[var(--color-border-card)] bg-[var(--color-bg-card-solid)] p-4">
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
      <div class="text-xs font-medium text-[var(--color-text-dimmed)]">Result</div>
      <div class="mt-1 text-lg font-semibold text-[var(--color-text-primary)]">{{ diceResult.roll.summary }}</div>
      <div v-if="diceResult.breakdown" class="mt-2 rounded-xl bg-[var(--color-bg-muted)] px-3 py-2 font-mono text-[12px] leading-5 text-[var(--color-text-secondary)]">
        {{ diceResult.breakdown }}
      </div>
    </div>
  </BaseCard>
</template>
