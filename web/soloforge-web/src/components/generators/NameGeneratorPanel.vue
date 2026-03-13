<script setup lang="ts">
import { ref } from 'vue'
import BaseCard from '../common/BaseCard.vue'
import BaseButton from '../common/BaseButton.vue'
import BaseSelect from '../common/BaseSelect.vue'
import BaseInput from '../common/BaseInput.vue'
import { useNameGenerator } from '../../composables/useNameGenerator'
import { copyToClipboard } from '../../composables/useRollMarkdown'

const { selectedStyleId, count, results, styles, generate } = useNameGenerator()

const copied = ref(false)

const styleOptions = styles.map(s => ({ value: s.id, label: s.name }))

async function handleCopy() {
  if (!results.value.length) return
  const text = results.value.join('\n')
  const success = await copyToClipboard(text)
  if (success) {
    copied.value = true
    setTimeout(() => { copied.value = false }, 1500)
  }
}
</script>

<template>
  <BaseCard title="Name Generator">
    <template #header>
      <div class="text-xs text-[var(--color-text-dimmed)]">Random names by style</div>
    </template>

    <div class="grid grid-cols-1 gap-3">
      <BaseSelect
        v-model="selectedStyleId"
        label="Style"
        :options="styleOptions"
      />

      <BaseInput
        v-model="count"
        label="Count"
        type="number"
        placeholder="5"
        @enter="generate"
      />

      <BaseButton @click="generate">
        Generate
      </BaseButton>
    </div>

    <div v-if="results.length" class="group/result mt-4 rounded-2xl border border-[var(--color-border-card)] bg-[var(--color-bg-card-solid)] p-4">
      <button
        type="button"
        class="float-right ml-2 rounded-lg p-1.5 text-[var(--color-text-dimmed)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)] transition opacity-0 group-hover/result:opacity-100 focus:opacity-100"
        :title="copied ? 'Copied!' : 'Copy to clipboard'"
        :aria-label="copied ? 'Copied!' : 'Copy to clipboard'"
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
      <div class="text-xs font-medium text-[var(--color-text-dimmed)]">Results</div>
      <ul class="mt-2 space-y-1">
        <li
          v-for="(name, i) in results"
          :key="i"
          class="text-sm font-semibold text-[var(--color-text-primary)]"
        >
          {{ name }}
        </li>
      </ul>
    </div>
  </BaseCard>
</template>
