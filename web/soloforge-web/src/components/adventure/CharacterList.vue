<script setup lang="ts">
import type { Character } from '../../types'
import BaseButton from '../common/BaseButton.vue'
import BaseInput from '../common/BaseInput.vue'
import { ref } from 'vue'

defineProps<{
  characters: Character[]
  loading: boolean
  loadingRemove: boolean
  apiOnline: boolean
}>()

const emit = defineEmits<{
  add: [name: string, description: string]
  remove: [name: string]
}>()

const characterName = ref('')
const characterDescription = ref('')

function handleAdd() {
  const name = characterName.value.trim()
  if (name) {
    emit('add', name, characterDescription.value.trim())
    characterName.value = ''
    characterDescription.value = ''
  }
}

function handleRemove(name: string) {
  const ok = window.confirm(`Remove character "${name}"?`)
  if (ok) {
    emit('remove', name)
  }
}
</script>

<template>
  <div>
    <div class="text-xs font-semibold text-[var(--color-text-secondary)]">Characters</div>
    <div class="mt-2 max-h-[220px] overflow-auto rounded-2xl border border-[var(--color-border-primary)] bg-[var(--color-bg-card-solid)]">
      <div v-if="characters.length === 0" class="p-3 text-sm text-[var(--color-text-muted)]">
        (No characters)
      </div>
      <ul v-else class="divide-y divide-[var(--color-border-primary)]">
        <li v-for="c in characters" :key="c.name" class="flex items-center justify-between gap-3 p-3">
          <div class="min-w-0">
            <div class="truncate text-sm font-semibold text-[var(--color-text-primary)]">{{ c.name }}</div>
            <div v-if="c.description" class="mt-0.5 truncate text-xs text-[var(--color-text-muted)]">{{ c.description }}</div>
          </div>
          <BaseButton
            variant="danger"
            size="sm"
            :disabled="loadingRemove || !apiOnline"
            @click="handleRemove(c.name)"
          >
            Remove
          </BaseButton>
        </li>
      </ul>
    </div>

    <div class="mt-3 grid grid-cols-1 gap-2">
      <BaseInput
        v-model="characterName"
        placeholder="Character name"
        @enter="handleAdd"
      />
      <BaseInput
        v-model="characterDescription"
        placeholder="Description (optional)"
        @enter="handleAdd"
      />
      <BaseButton
        :disabled="loading || !characterName.trim() || !apiOnline"
        :loading="loading"
        @click="handleAdd"
      >
        Add character
      </BaseButton>
    </div>
  </div>
</template>
