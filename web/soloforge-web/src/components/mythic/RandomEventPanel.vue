<script setup lang="ts">
import { ref } from 'vue'
import BaseCard from '../common/BaseCard.vue'
import BaseButton from '../common/BaseButton.vue'
import BaseInput from '../common/BaseInput.vue'
import { useMythic } from '../../composables/useMythic'
import { useAdventure } from '../../composables/useAdventure'
import { useCampaign } from '../../composables/useCampaign'
import { useToolActions } from '../../composables/useToolActions'
import { randomEventToMarkdown, copyToClipboard } from '../../composables/useRollMarkdown'

const { randomResult, runRandomEvent, loading } = useMythic()
const adventure = useAdventure()
const campaign = useCampaign()
const { apiOnline, runAction, clearError, setError } = useToolActions()

const copied = ref(false)
const newNpcName = ref('')
const newNpcDescription = ref('')

function handleRoll() {
  void runAction(() => runRandomEvent())
}

async function handleCopy() {
  if (!randomResult.value) return
  const success = await copyToClipboard(randomEventToMarkdown(randomResult.value))
  if (success) {
    copied.value = true
    setTimeout(() => { copied.value = false }, 1500)
  }
}

async function handleAddNpc() {
  if (adventure.loading.addCharacter || !apiOnline.value) return
  const name = newNpcName.value.trim()
  if (!name) return
  clearError()
  try {
    await adventure.addCharacter(name, newNpcDescription.value.trim() || undefined)
    await campaign.refreshState()
    await campaign.refreshCampaigns()
    newNpcName.value = ''
    newNpcDescription.value = ''
  } catch (err) {
    setError(err)
  }
}
</script>

<template>
  <BaseCard title="Random Event">
    <template #header>
      <BaseButton
        size="sm"
        :disabled="loading.randomEvent || !apiOnline"
        :loading="loading.randomEvent"
        @click="handleRoll"
      >
        Roll
      </BaseButton>
    </template>

    <div v-if="randomResult" class="group/result rounded-2xl border border-[var(--color-border-card)] bg-[var(--color-bg-card-solid)] p-4">
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
      <div class="text-xs font-medium text-[var(--color-text-dimmed)]">Focus</div>
      <div class="mt-1 text-base font-semibold text-[var(--color-text-primary)]">{{ randomResult.eventFocus }}</div>
      <div class="mt-3 text-xs font-medium text-[var(--color-text-dimmed)]">Action</div>
      <div class="mt-1 text-base font-semibold text-[var(--color-text-primary)]">{{ randomResult.eventAction }}</div>

      <div v-if="randomResult.selectedCharacter" class="mt-3 text-sm text-[var(--color-text-secondary)]">
        Character: <span class="font-semibold text-[var(--color-text-primary)]">{{ randomResult.selectedCharacter }}</span>
      </div>
      <div v-if="randomResult.selectedThread" class="mt-1 text-sm text-[var(--color-text-secondary)]">
        Thread: <span class="font-semibold text-[var(--color-text-primary)]">{{ randomResult.selectedThread }}</span>
      </div>
      <div v-if="randomResult.listWasEmpty" class="mt-3 text-sm text-[var(--color-text-secondary)]">(List was empty)</div>
      <div v-if="randomResult.isNewNpc" class="mt-3 text-sm font-semibold text-[var(--color-text-warning)]">
        New NPC: add them to your character list.
      </div>
    </div>

    <div v-if="randomResult?.isNewNpc" class="mt-4 rounded-2xl border border-[var(--color-border-warning)] bg-[var(--color-bg-warning-subtle)] p-4">
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
        :disabled="adventure.loading.addCharacter || !newNpcName.trim() || !apiOnline"
        :loading="adventure.loading.addCharacter"
        @click="handleAddNpc"
      >
        Add NPC
      </BaseButton>
    </div>
  </BaseCard>
</template>
