<script setup lang="ts">
import type { RandomEventResult } from '../../types'
import BaseCard from '../common/BaseCard.vue'
import BaseButton from '../common/BaseButton.vue'
import BaseInput from '../common/BaseInput.vue'
import { ref } from 'vue'

defineProps<{
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

function handleAddNpc() {
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

    <div v-if="result" class="rounded-2xl border border-[var(--color-border-card)] bg-[var(--color-bg-card-solid)] p-4">
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
