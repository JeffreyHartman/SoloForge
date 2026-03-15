<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import BaseCard from '../common/BaseCard.vue'
import BaseButton from '../common/BaseButton.vue'
import JournalToolbar from './JournalToolbar.vue'
import WysiwygEditor from './WysiwygEditor.vue'
import { useJournal } from '../../composables/useJournal'
import { useJournalPrefs, FONT_FAMILIES } from '../../composables/useJournalPrefs'

const props = defineProps<{
  campaignId: string | null
  loading: boolean
  apiOnline: boolean
}>()

const content = defineModel<string>('content')

defineEmits<{
  reload: []
}>()

const wysiwygRef = ref<InstanceType<typeof WysiwygEditor> | null>(null)
const { saveStatus, flushSave } = useJournal()
const { prefs } = useJournalPrefs()

const fontStyle = computed(() => ({
  fontFamily: FONT_FAMILIES[prefs.fontFamily] ?? FONT_FAMILIES.mono,
  fontSize: `${prefs.fontSize}px`,
}))

const statusText = computed(() => {
  if (saveStatus.value === 'saving') return 'Saving...'
  if (saveStatus.value === 'unsaved') return 'Unsaved'
  return 'Saved'
})

const statusClass = computed(() => {
  if (saveStatus.value === 'saving') return 'text-[var(--color-text-info)]'
  if (saveStatus.value === 'unsaved') return 'text-[var(--color-text-warning)]'
  return 'text-[var(--color-text-dimmed)]'
})

// Keyboard shortcuts
function onKeydown(e: KeyboardEvent) {
  const mod = e.ctrlKey || e.metaKey
  if (!mod || e.shiftKey || e.key.toLowerCase() !== 'e') return

  e.preventDefault()
  prefs.mode = prefs.mode === 'edit' ? 'preview' : 'edit'
}

onMounted(() => document.addEventListener('keydown', onKeydown))
onUnmounted(() => {
  document.removeEventListener('keydown', onKeydown)
  flushSave()
})
</script>

<template>
  <BaseCard title="Journal">
    <template #header>
      <div class="flex items-center gap-2">
        <span class="text-xs transition-colors" :class="statusClass">{{ statusText }}</span>
        <BaseButton
          variant="secondary"
          size="sm"
          :disabled="loading || !apiOnline || !campaignId"
          @click="$emit('reload')"
        >
          Reload
        </BaseButton>
      </div>
    </template>

    <JournalToolbar
      :mode="prefs.mode"
      :enhanced="prefs.enhanced"
      :font-family="prefs.fontFamily"
      :font-size="prefs.fontSize"
      @update:mode="prefs.mode = $event"
      @update:enhanced="prefs.enhanced = $event"
      @update:font-family="prefs.fontFamily = $event"
      @update:font-size="prefs.fontSize = $event"
    />

    <!-- Edit mode -->
    <textarea
      v-if="prefs.mode === 'edit'"
      v-model="content"
      aria-label="Journal content"
      class="h-[calc(100vh-20rem)] min-h-[420px] w-full resize-none rounded-2xl border border-[var(--color-border-primary)] bg-[var(--color-bg-input)] p-4 leading-relaxed text-[var(--color-text-primary)] shadow-sm outline-none transition placeholder:text-[var(--color-text-dimmed)] focus:border-[var(--color-text-dimmed)] focus:shadow"
      :style="fontStyle"
      :placeholder="campaignId ? 'Write your journal in markdown...' : 'Load or create a campaign first.'"
      :disabled="!campaignId"
      @blur="flushSave"
    />

    <!-- Preview / WYSIWYG mode (single pane) -->
    <div
      v-else
      class="flex h-[calc(100vh-20rem)] min-h-[420px] flex-col overflow-y-auto rounded-2xl border border-[var(--color-border-primary)] bg-[var(--color-bg-input)] p-4 shadow-sm outline-none transition focus-within:border-[var(--color-text-dimmed)] focus-within:shadow"
      :style="fontStyle"
      @click.self="wysiwygRef?.focusEnd()"
    >
      <WysiwygEditor
        ref="wysiwygRef"
        :content="content"
        :font-style="fontStyle"
        :disabled="!campaignId"
        :placeholder="campaignId ? 'Write your journal in markdown...' : 'Load or create a campaign first.'"
        aria-label="Journal content"
        @update:content="content = $event"
      />
    </div>

    <div class="mt-2 text-xs text-[var(--color-text-dimmed)]">
      Saved as markdown in <code class="rounded bg-[var(--color-bg-muted)] px-1 py-0.5 font-mono text-[11px]">saves/</code>. Compatible with Obsidian and other markdown editors.
    </div>
  </BaseCard>
</template>
