<script setup lang="ts">
import type { Character, Thread } from '../types'
import BaseCard from '../components/common/BaseCard.vue'
import CharacterList from '../components/adventure/CharacterList.vue'
import ThreadList from '../components/adventure/ThreadList.vue'

defineProps<{
  characters: Character[]
  activeThreads: Thread[]
  closedThreads: Thread[]
  loadingAddCharacter: boolean
  loadingRemoveCharacter: boolean
  loadingAddThread: boolean
  loadingCloseThread: boolean
  loadingReopenThread: boolean
  apiOnline: boolean
}>()

const emit = defineEmits<{
  addCharacter: [name: string, description: string]
  removeCharacter: [name: string]
  addThread: [name: string, description: string]
  closeThread: [name: string]
  reopenThread: [name: string]
}>()
</script>

<template>
  <BaseCard title="Adventure Lists">
    <template #header>
      <div class="text-xs text-[var(--color-text-dimmed)]">
        {{ characters.length }} characters · {{ activeThreads.length }} active threads · {{ closedThreads.length }} closed
      </div>
    </template>

    <div class="grid grid-cols-1 gap-6 sm:grid-cols-2">
      <CharacterList
        :characters="characters"
        :loading="loadingAddCharacter"
        :loading-remove="loadingRemoveCharacter"
        :api-online="apiOnline"
        @add="(name, desc) => emit('addCharacter', name, desc)"
        @remove="(name) => emit('removeCharacter', name)"
      />

      <ThreadList
        :active-threads="activeThreads"
        :closed-threads="closedThreads"
        :loading-add="loadingAddThread"
        :loading-close="loadingCloseThread"
        :loading-reopen="loadingReopenThread"
        :api-online="apiOnline"
        @add="(name, desc) => emit('addThread', name, desc)"
        @close="(name) => emit('closeThread', name)"
        @reopen="(name) => emit('reopenThread', name)"
      />
    </div>
  </BaseCard>
</template>
