<script setup lang="ts">
import type { RandomEventResult } from '../../types'
import BaseCard from '../common/BaseCard.vue'
import BaseButton from '../common/BaseButton.vue'
import BaseInput from '../common/BaseInput.vue'
import { ref } from 'vue'
import { randomEventToMarkdown, copyToClipboard } from '../../composables/useRollMarkdown'

const copied = ref(false)

const props = defineProps<{
  result: RandomEventResult | null
  loading: boolean
  loadingAddNpc: boolean
  apiOnline: boolean
}>()

const emit = defineEmits<{
  roll: []
  addNpc: [name: string, description: string]
}>()

const newNpcName = ref('')
const newNpcDescription = ref('')

async function handleCopy() {
  if (!props.result) return
  const success = await copyToClipboard(randomEventToMarkdown(props.result))
  if (success) {
    copied.value = true
    setTimeout(() => { copied.value = false }, 1500)
  }
}

function handleAddNpc() {
  if (props.loadingAddNpc || !props.apiOnline) return
  const name = newNpcName.value.trim()
  if (name) {
    emit('addNpc', name, newNpcDescription.value.trim())
    newNpcName.value = ''
    newNpcDescription.value = ''
  }
}
</script>

<template>
  <BaseCard title="Random Event">
    <template #header>
      <BaseButton
        size="sm"
        :disabled="loading || !apiOnline"
        :loading="loading"
        @click="$emit('roll')"
      >
        Roll
      </BaseButton>
    </template>

    <div v-if="result" class="group/result rounded-2xl border border-[var(--color-border-card)] bg-[var(--color-bg-card-solid)] p-4">
      <button
        class="float-right ml-2 rounded-lg p-1.5 text-[var(--color-text-dimmed)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)] transition opacity-0 group-hover/result:opacity-100"
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
      <div class="text-xs font-medium text-[var(--color-text-dimmed)]">Focus</div>
      <div class="mt-1 text-base font-semibold text-[var(--color-text-primary)]">{{ result.eventFocus }}</div>
      <div class="mt-3 text-xs font-medium text-[var(--color-text-dimmed)]">Action</div>
      <div class="mt-1 text-base font-semibold text-[var(--color-text-primary)]">{{ result.eventAction }}</div>

      <div v-if="result.selectedCharacter" class="mt-3 text-sm text-[var(--color-text-secondary)]">
        Character: <span class="font-semibold text-[var(--color-text-primary)]">{{ result.selectedCharacter }}</span>
      </div>
      <div v-if="result.selectedThread" class="mt-1 text-sm text-[var(--color-text-secondary)]">
        Thread: <span class="font-semibold text-[var(--color-text-primary)]">{{ result.selectedThread }}</span>
      </div>
      <div v-if="result.listWasEmpty" class="mt-3 text-sm text-[var(--color-text-secondary)]">(List was empty)</div>
      <div v-if="result.isNewNpc" class="mt-3 text-sm font-semibold text-[var(--color-text-warning)]">
        New NPC: add them to your character list.
      </div>
    </div>

    <div v-if="result?.isNewNpc" class="mt-4 rounded-2xl border border-[var(--color-border-warning)] bg-[var(--color-bg-warning-subtle)] p-4">
      <div class="text-xs font-semibold text-[var(--color-text-warning)]">Add NPC</div>
      <div class="mt-3 grid grid-cols-1 gap-3 sm:grid-cols-5">
        <div class="sm:col-span-3">
          <BaseInput
            v-model="newNpcName"
            label="Name"
            placeholder="NPC name"
            @enter="handleAddNpc"
          />
        </div>
        <div class="sm:col-span-2">
          <BaseInput
            v-model="newNpcDescription"
            label="Description"
            placeholder="Optional"
          />
        </div>
      </div>
      <BaseButton
        class="mt-3 w-full"
        variant="warning"
        :disabled="loadingAddNpc || !newNpcName.trim() || !apiOnline"
        :loading="loadingAddNpc"
        @click="handleAddNpc"
      >
        Add NPC
      </BaseButton>
    </div>
  </BaseCard>
</template>
