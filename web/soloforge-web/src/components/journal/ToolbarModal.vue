<script setup lang="ts">
import FateCheckPanel from '../mythic/FateCheckPanel.vue'
import SceneCheckPanel from '../mythic/SceneCheckPanel.vue'
import MeaningPanel from '../mythic/MeaningPanel.vue'
import DiceRollerPanel from '../mythic/DiceRollerPanel.vue'
import { getPinnableTool } from '../../tools/pinnableTools'
import { computed } from 'vue'

const props = defineProps<{
  toolId: string | null
}>()

const emit = defineEmits<{
  close: []
}>()

const toolLabel = computed(() => {
  if (!props.toolId) return ''
  return getPinnableTool(props.toolId)?.label ?? ''
})
</script>

<template>
  <div
    v-if="toolId"
    class="absolute inset-0 z-50 flex items-center justify-center bg-black/30 backdrop-blur-sm"
    @click.self="emit('close')"
    @keydown.escape="emit('close')"
  >
    <div class="w-96 max-h-[80%] overflow-y-auto rounded-2xl border border-[var(--color-border-primary)] bg-[var(--color-bg-card-solid)] p-5 shadow-xl">
      <div class="mb-3 flex items-center justify-between">
        <h3 class="text-sm font-semibold text-[var(--color-text-primary)]">{{ toolLabel }}</h3>
        <button
          type="button"
          class="rounded-lg p-1 text-[var(--color-text-dimmed)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)] transition"
          aria-label="Close tool modal"
          @click="emit('close')"
        >
          <svg class="h-4 w-4" viewBox="0 0 14 14" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M2 2l10 10M12 2L2 12" />
          </svg>
        </button>
      </div>

      <FateCheckPanel v-if="toolId === 'fate-check'" mode="toolbar" @rolled="emit('close')" />
      <SceneCheckPanel v-else-if="toolId === 'scene-check'" mode="toolbar" @rolled="emit('close')" />
      <MeaningPanel v-else-if="toolId === 'meaning'" mode="toolbar" @rolled="emit('close')" />
      <DiceRollerPanel v-else-if="toolId === 'dice-roller'" mode="toolbar" @rolled="emit('close')" />
    </div>
  </div>
</template>
