<script setup lang="ts">
import { ref } from 'vue'
import type { MeaningMode } from '../../types'
import BaseCard from '../common/BaseCard.vue'
import BaseButton from '../common/BaseButton.vue'
import BaseInput from '../common/BaseInput.vue'
import BaseSelect from '../common/BaseSelect.vue'
import { useMythic } from '../../composables/useMythic'
import { useTables } from '../../composables/useTables'
import { useToolActions } from '../../composables/useToolActions'
import { meaningToMarkdown, quickSetToMarkdown, copyToClipboard } from '../../composables/useRollMarkdown'

const {
  meaningMode, meaningContext, meaningTableId, meaningFusionTable1, meaningFusionTable2,
  meaningQuickSetId, meaningResult, meaningMeta, quickSetResult, runMeaning, loading,
} = useMythic()
const { tableGroups, quickSets } = useTables()
const { apiOnline, runAction } = useToolActions()

const copiedMeaning = ref(false)
const copiedQuickSet = ref(false)

function handleRoll() {
  void runAction(() => runMeaning())
}

async function handleCopyMeaning() {
  if (!meaningResult.value) return
  const success = await copyToClipboard(meaningToMarkdown(meaningResult.value))
  if (success) {
    copiedMeaning.value = true
    setTimeout(() => { copiedMeaning.value = false }, 1500)
  }
}

async function handleCopyQuickSet() {
  if (!quickSetResult.value) return
  const success = await copyToClipboard(quickSetToMarkdown(quickSetResult.value))
  if (success) {
    copiedQuickSet.value = true
    setTimeout(() => { copiedQuickSet.value = false }, 1500)
  }
}

const modes: { id: MeaningMode; label: string }[] = [
  { id: 'action', label: 'Action' },
  { id: 'description', label: 'Description' },
  { id: 'table', label: 'Table' },
  { id: 'fusion', label: 'Fusion' },
  { id: 'quickSet', label: 'Quick set' },
]
</script>

<template>
  <BaseCard title="Meaning">
    <template #header>
      <div class="text-xs text-[var(--color-text-dimmed)]">Action/Description/Tables/Fusion/Quick Sets</div>
    </template>

    <div class="flex flex-wrap gap-2" role="group" aria-label="Meaning mode">
      <button
        v-for="m in modes"
        :key="m.id"
        type="button"
        :aria-pressed="meaningMode === m.id"
        class="rounded-full px-3 py-1.5 text-xs font-semibold shadow-sm transition"
        :class="
          meaningMode === m.id
            ? 'bg-[var(--color-bg-accent)] text-[var(--color-text-inverted)]'
            : 'border border-[var(--color-border-primary)] bg-[var(--color-bg-card-solid)] text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)]'
        "
        @click="meaningMode = m.id"
      >
        {{ m.label }}
      </button>
    </div>

    <div class="mt-4 grid grid-cols-1 gap-3">
      <BaseInput
        v-model="meaningContext"
        label="Context (optional)"
        placeholder="What are you trying to understand?"
        @enter="handleRoll"
      />

      <div v-if="meaningMode === 'table'">
        <BaseSelect v-model="meaningTableId" label="Table" :groups="tableGroups" />
      </div>

      <div v-else-if="meaningMode === 'fusion'" class="grid grid-cols-1 gap-3 sm:grid-cols-2">
        <BaseSelect v-model="meaningFusionTable1" label="Table 1" :groups="tableGroups" />
        <BaseSelect v-model="meaningFusionTable2" label="Table 2" :groups="tableGroups" />
      </div>

      <div v-else-if="meaningMode === 'quickSet'">
        <BaseSelect v-model="meaningQuickSetId" label="Quick set" :options="quickSets.map(q => ({ value: q.id, label: q.name }))" />
      </div>

      <BaseButton
        :disabled="loading.meaning || !apiOnline"
        :loading="loading.meaning"
        @click="handleRoll"
      >
        Roll
      </BaseButton>
    </div>

    <div v-if="meaningResult" class="group/result mt-4 rounded-2xl border border-[var(--color-border-card)] bg-[var(--color-bg-card-solid)] p-4">
      <button
        type="button"
        class="float-right ml-2 rounded-lg p-1.5 text-[var(--color-text-dimmed)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)] transition opacity-0 group-hover/result:opacity-100 focus:opacity-100"
        :title="copiedMeaning ? 'Copied!' : 'Copy as markdown'"
        :aria-label="copiedMeaning ? 'Copied!' : 'Copy as markdown'"
        @click="handleCopyMeaning"
      >
        <svg v-if="!copiedMeaning" class="h-4 w-4" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5">
          <rect x="5" y="5" width="9" height="9" rx="1.5" />
          <path d="M11 5V3.5A1.5 1.5 0 009.5 2h-6A1.5 1.5 0 002 3.5v6A1.5 1.5 0 003.5 11H5" />
        </svg>
        <svg v-else class="h-4 w-4 text-[var(--color-text-success)]" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2">
          <path d="M3 8l3 3 7-7" />
        </svg>
      </button>
      <div class="text-xs font-medium text-[var(--color-text-dimmed)]">Result</div>
      <div class="mt-1 text-lg font-semibold text-[var(--color-text-primary)]">{{ meaningResult.combined }}</div>
      <div class="mt-1 text-xs text-[var(--color-text-muted)]">
        <span v-if="meaningMeta">{{ meaningMeta }}</span>
        <span v-else>{{ meaningResult.tableName }}</span>
      </div>
      <div class="mt-3 grid grid-cols-1 gap-2 sm:grid-cols-2">
        <div class="rounded-xl bg-[var(--color-bg-muted)] px-3 py-2">
          <div class="text-[11px] font-medium text-[var(--color-text-dimmed)]">Word 1</div>
          <div class="mt-0.5 text-sm font-semibold text-[var(--color-text-primary)]">{{ meaningResult.word1 }}</div>
        </div>
        <div class="rounded-xl bg-[var(--color-bg-muted)] px-3 py-2">
          <div class="text-[11px] font-medium text-[var(--color-text-dimmed)]">Word 2</div>
          <div class="mt-0.5 text-sm font-semibold text-[var(--color-text-primary)]">{{ meaningResult.word2 }}</div>
        </div>
      </div>
    </div>

    <div v-if="quickSetResult" class="group/qs mt-4 rounded-2xl border border-[var(--color-border-card)] bg-[var(--color-bg-card-solid)] p-4">
      <button
        type="button"
        class="float-right ml-2 rounded-lg p-1.5 text-[var(--color-text-dimmed)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)] transition opacity-0 group-hover/qs:opacity-100 focus:opacity-100"
        :title="copiedQuickSet ? 'Copied!' : 'Copy as markdown'"
        :aria-label="copiedQuickSet ? 'Copied!' : 'Copy as markdown'"
        @click="handleCopyQuickSet"
      >
        <svg v-if="!copiedQuickSet" class="h-4 w-4" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5">
          <rect x="5" y="5" width="9" height="9" rx="1.5" />
          <path d="M11 5V3.5A1.5 1.5 0 009.5 2h-6A1.5 1.5 0 002 3.5v6A1.5 1.5 0 003.5 11H5" />
        </svg>
        <svg v-else class="h-4 w-4 text-[var(--color-text-success)]" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2">
          <path d="M3 8l3 3 7-7" />
        </svg>
      </button>
      <div class="text-xs font-medium text-[var(--color-text-dimmed)]">Quick set</div>
      <div class="mt-1 text-base font-semibold text-[var(--color-text-primary)]">{{ quickSetResult.quickSet.name }}</div>
      <div class="mt-1 text-xs text-[var(--color-text-muted)]">{{ quickSetResult.quickSet.description }}</div>

      <div class="mt-4 rounded-xl border border-[var(--color-border-primary)] bg-[var(--color-bg-muted)] p-3">
        <div v-for="r in quickSetResult.results" :key="r.label" class="py-1 text-sm">
          <span class="font-semibold text-[var(--color-text-primary)]">{{ r.label }}:</span>
          <span class="text-[var(--color-text-secondary)]"> {{ r.combined }}</span>
        </div>
      </div>
    </div>
  </BaseCard>
</template>
