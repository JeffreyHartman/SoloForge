<script setup lang="ts">
import type { CampaignInfo } from '../../types'
import BaseCard from '../common/BaseCard.vue'
import BaseButton from '../common/BaseButton.vue'
import BaseInput from '../common/BaseInput.vue'

defineProps<{
  currentCampaign: CampaignInfo | null
  chaos: number
  loading: boolean
  apiOnline: boolean
}>()

const chaosDraft = defineModel<number>('chaosDraft')
const engineDraft = defineModel<string>('engineDraft')

defineEmits<{
  apply: []
}>()

function formatDate(value: string | null | undefined): string {
  if (!value) return ''
  const d = new Date(value)
  if (Number.isNaN(d.getTime())) return value
  return d.toLocaleString()
}
</script>

<template>
  <BaseCard>
    <div class="flex items-start justify-between gap-3">
      <div>
        <div class="text-xs font-medium tracking-wide text-[var(--color-text-dimmed)]">Current campaign</div>
        <div class="mt-1 text-lg font-semibold text-[var(--color-text-primary)]">
          {{ currentCampaign?.name ?? 'No campaign loaded' }}
        </div>
        <div v-if="currentCampaign" class="mt-1 text-xs text-[var(--color-text-muted)]">
          Last played: {{ formatDate(currentCampaign.lastPlayed) }}
        </div>
      </div>
      <div class="rounded-2xl border border-[var(--color-border-card)] bg-[var(--color-bg-card-solid)] px-3 py-2 text-center shadow-sm">
        <div class="text-[11px] font-medium text-[var(--color-text-dimmed)]">Chaos</div>
        <div class="mt-0.5 text-lg font-semibold tabular-nums text-[var(--color-text-primary)]">{{ chaos }}</div>
      </div>
    </div>

    <div class="mt-5 grid grid-cols-1 gap-3 sm:grid-cols-5">
      <div class="sm:col-span-3">
        <BaseInput
          v-model="chaosDraft"
          label="Set chaos factor (1-9)"
          type="number"
          :min="1"
          :max="9"
        />
      </div>
      <div class="sm:col-span-2 sm:flex sm:items-end">
        <BaseButton
          class="w-full"
          :disabled="loading || !apiOnline"
          :loading="loading"
          @click="$emit('apply')"
        >
          Apply
        </BaseButton>
      </div>
    </div>

    <div class="mt-3">
      <BaseInput v-model="engineDraft" label="Engine" />
    </div>
  </BaseCard>
</template>
