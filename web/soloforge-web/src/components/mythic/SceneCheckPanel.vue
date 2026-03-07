<script setup lang="ts">
import type { SceneCheckResponse } from '../../types'
import BaseCard from '../common/BaseCard.vue'
import BaseButton from '../common/BaseButton.vue'
import BaseInput from '../common/BaseInput.vue'

defineProps<{
  chaos: number
  result: SceneCheckResponse | null
  loading: boolean
  apiOnline: boolean
}>()

const context = defineModel<string>('context')

defineEmits<{
  roll: []
}>()
</script>

<template>
  <BaseCard title="Scene Check">
    <template #header>
      <div class="text-xs text-[var(--color-text-dimmed)]">Chaos {{ chaos }}</div>
    </template>

    <div class="grid grid-cols-1 gap-3">
      <BaseInput
        v-model="context"
        label="Scene context (optional)"
        placeholder="What is the scene setup?"
        @enter="$emit('roll')"
      />

      <BaseButton
        :disabled="loading || !apiOnline"
        :loading="loading"
        @click="$emit('roll')"
      >
        Check scene
      </BaseButton>
    </div>

    <div v-if="result" class="mt-4 rounded-2xl border border-[var(--color-border-card)] bg-[var(--color-bg-card-solid)] p-4">
      <div class="flex items-start justify-between gap-3">
        <div>
          <div class="text-xs font-medium text-[var(--color-text-dimmed)]">Result</div>
          <div
            class="mt-1 text-lg font-semibold"
            :class="
              result.scene.result === 'Normal Scene'
                ? 'text-[var(--color-text-success)]'
                : result.scene.result.includes('Altered')
                  ? 'text-[var(--color-text-warning)]'
                  : result.scene.result.includes('Interrupt')
                    ? 'text-[var(--color-text-danger)]'
                    : 'text-[var(--color-text-primary)]'
            "
          >
            {{ result.scene.result }}
          </div>
          <div class="mt-1 text-xs text-[var(--color-text-muted)]">
            Roll {{ result.scene.roll }} · Chaos {{ result.chaos }}
          </div>
        </div>
      </div>

      <div v-if="result.scene.sceneAdjustment" class="mt-4 rounded-xl border border-[var(--color-border-warning)] bg-[var(--color-bg-warning-subtle)] p-3">
        <div class="text-xs font-semibold text-[var(--color-text-warning)]">Scene adjustment</div>
        <div class="mt-1 text-sm font-semibold text-[var(--color-text-primary)]">{{ result.scene.sceneAdjustment }}</div>
      </div>

      <div v-if="result.scene.randomEvent" class="mt-4 rounded-xl border border-[var(--color-border-info)] bg-[var(--color-bg-info)] p-3">
        <div class="text-xs font-semibold text-[var(--color-text-info)]">Random event</div>
        <div class="mt-1 text-sm font-semibold text-[var(--color-text-primary)]">
          {{ result.scene.randomEvent.eventFocus }}: {{ result.scene.randomEvent.eventAction }}
        </div>
        <div v-if="result.scene.randomEvent.selectedCharacter" class="mt-1 text-xs text-[var(--color-text-secondary)]">
          Character: {{ result.scene.randomEvent.selectedCharacter }}
        </div>
        <div v-if="result.scene.randomEvent.selectedThread" class="mt-1 text-xs text-[var(--color-text-secondary)]">
          Thread: {{ result.scene.randomEvent.selectedThread }}
        </div>
        <div v-if="result.scene.randomEvent.listWasEmpty" class="mt-1 text-xs text-[var(--color-text-secondary)]">
          (List was empty)
        </div>
      </div>
    </div>
  </BaseCard>
</template>
