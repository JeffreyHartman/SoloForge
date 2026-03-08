<script setup lang="ts">
import RollPanel from './RollPanel.vue'
import MarkdownBlock from './MarkdownBlock.vue'
import type { JournalSegment } from '../../composables/useJournalParser'

defineProps<{
  content: string | undefined
  enhanced: boolean
  segments: JournalSegment[]
  collapsedIds: Set<string>
  emptyMessage: string
}>()

defineEmits<{
  toggle: [id: string]
  delete: [id: string]
  navigate: [path: string]
}>()
</script>

<template>
  <div v-if="!content?.trim()" class="text-[var(--color-text-dimmed)]">
    {{ emptyMessage }}
  </div>
  <template v-else-if="enhanced">
    <template v-for="segment in segments" :key="segment.id">
      <MarkdownBlock v-if="segment.type === 'text'" :content="segment.raw" @navigate="$emit('navigate', $event)" />
      <RollPanel
        v-else
        :segment="segment"
        :collapsed="collapsedIds.has(segment.id)"
        @toggle="$emit('toggle', segment.id)"
        @delete="$emit('delete', segment.id)"
      />
    </template>
  </template>
  <MarkdownBlock v-else :content="content" @navigate="$emit('navigate', $event)" />
</template>
