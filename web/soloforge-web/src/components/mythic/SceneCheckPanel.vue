<script setup lang="ts">
import { ref } from 'vue'
import BaseCard from '../common/BaseCard.vue'
import BaseButton from '../common/BaseButton.vue'
import BaseInput from '../common/BaseInput.vue'
import { useMythic } from '../../composables/useMythic'
import { useCampaign } from '../../composables/useCampaign'
import { useToolActions } from '../../composables/useToolActions'
import { sceneCheckToMarkdown, copyToClipboard } from '../../composables/useRollMarkdown'

const { sceneContext, sceneResult, runSceneCheck, loading } = useMythic()
const { session } = useCampaign()
const { apiOnline, runAction } = useToolActions()

const copied = ref(false)

function handleRoll() {
  void runAction(() => runSceneCheck())
}

async function handleCopy() {
  if (!sceneResult.value) return
  const success = await copyToClipboard(sceneCheckToMarkdown(sceneResult.value))
  if (success) {
    copied.value = true
    setTimeout(() => { copied.value = false }, 1500)
  }
}
</script>

<template>
  <BaseCard title="Scene Check">
    <template #header>
      <div class="text-xs text-[var(--color-text-dimmed)]">Chaos {{ session.chaos }}</div>
    </template>

    <div class="grid grid-cols-1 gap-3">
      <BaseInput
        v-model="sceneContext"
        label="Scene context (optional)"
        placeholder="What is the scene setup?"
        @enter="handleRoll"
      />

      <BaseButton
        :disabled="loading.sceneCheck || !apiOnline"
        :loading="loading.sceneCheck"
        @click="handleRoll"
      >
        Check scene
      </BaseButton>
    </div>

    <div v-if="sceneResult" class="group/result mt-4 rounded-2xl border border-[var(--color-border-card)] bg-[var(--color-bg-card-solid)] p-4">
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
      <div class="flex items-start justify-between gap-3">
        <div>
          <div class="text-xs font-medium text-[var(--color-text-dimmed)]">Result</div>
          <div
            class="mt-1 text-lg font-semibold"
            :class="
              sceneResult.scene.result === 'Normal Scene'
                ? 'text-[var(--color-text-success)]'
                : sceneResult.scene.result.includes('Altered')
                  ? 'text-[var(--color-text-warning)]'
                  : sceneResult.scene.result.includes('Interrupt')
                    ? 'text-[var(--color-text-danger)]'
                    : 'text-[var(--color-text-primary)]'
            "
          >
            {{ sceneResult.scene.result }}
          </div>
          <div class="mt-1 text-xs text-[var(--color-text-muted)]">
            Roll {{ sceneResult.scene.roll }} · Chaos {{ sceneResult.chaos }}
          </div>
        </div>
      </div>

      <div v-if="sceneResult.scene.sceneAdjustment" class="mt-4 rounded-xl border border-[var(--color-border-warning)] bg-[var(--color-bg-warning-subtle)] p-3">
        <div class="text-xs font-semibold text-[var(--color-text-warning)]">Scene adjustment</div>
        <div class="mt-1 text-sm font-semibold text-[var(--color-text-primary)]">{{ sceneResult.scene.sceneAdjustment }}</div>
      </div>

      <div v-if="sceneResult.scene.randomEvent" class="mt-4 rounded-xl border border-[var(--color-border-info)] bg-[var(--color-bg-info)] p-3">
        <div class="text-xs font-semibold text-[var(--color-text-info)]">Random event</div>
        <div class="mt-1 text-sm font-semibold text-[var(--color-text-primary)]">
          {{ sceneResult.scene.randomEvent.eventFocus }}: {{ sceneResult.scene.randomEvent.eventAction }}
        </div>
        <div v-if="sceneResult.scene.randomEvent.selectedCharacter" class="mt-1 text-xs text-[var(--color-text-secondary)]">
          Character: {{ sceneResult.scene.randomEvent.selectedCharacter }}
        </div>
        <div v-if="sceneResult.scene.randomEvent.selectedThread" class="mt-1 text-xs text-[var(--color-text-secondary)]">
          Thread: {{ sceneResult.scene.randomEvent.selectedThread }}
        </div>
        <div v-if="sceneResult.scene.randomEvent.listWasEmpty" class="mt-1 text-xs text-[var(--color-text-secondary)]">
          (List was empty)
        </div>
      </div>
    </div>
  </BaseCard>
</template>
