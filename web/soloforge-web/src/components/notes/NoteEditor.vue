<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, watch } from 'vue'
import JournalToolbar from '../journal/JournalToolbar.vue'
import WysiwygEditor from '../journal/WysiwygEditor.vue'
import WikiLinkAutocomplete from './WikiLinkAutocomplete.vue'
import { useJournalPrefs, FONT_FAMILIES } from '../../composables/useJournalPrefs'
import { isNearBottom } from '../journal/editorState'
import { useNotes } from '../../composables/useNotes'
import { useCampaign } from '../../composables/useCampaign'
import { apiSend } from '../../composables/useApi'
import { useToast } from '../../composables/useToast'

const props = defineProps<{
  apiOnline: boolean
}>()

const textareaRef = ref<HTMLTextAreaElement | null>(null)
const wysiwygRef = ref<InstanceType<typeof WysiwygEditor> | null>(null)
const scrollContainerRef = ref<HTMLElement | null>(null)
const { activeNotePath, activeNoteContent, activeNoteFileName, saveStatus, allPaths, openNote, resolveNotePath, flushSave } = useNotes()
const { currentCampaign, refreshState } = useCampaign()
const { addToast } = useToast()

async function updateJournalPref(key: 'autoJournalEvents' | 'autoJournalDiceRolls', value: boolean) {
  await apiSend('/api/campaigns/journal-prefs', 'PUT', { [key]: value })
  await refreshState()
}

/** Navigates to a wiki-linked note by opening it in the editor. */
async function handleNavigate(path: string) {
  try {
    const resolved = resolveNotePath(path)
    await openNote(resolved)
  } catch {
    const name = path.endsWith('.md') ? path.slice(0, -3) : path
    addToast({ title: 'Note not found', detail: `"${name}" does not exist.`, variant: 'warning' })
  }
}

const { prefs } = useJournalPrefs()

const STICKY_SCROLL_THRESHOLD = 80

// Sticky scroll: when activeNoteContent changes (e.g., a tool appends a roll
// result), auto-scroll to the new bottom only if the user was already near
// the bottom before the change. Sync flush captures the pre-update scroll
// position; rAF schedules the scroll after Tiptap and Vue have both rendered.
watch(activeNoteContent, () => {
  const el = scrollContainerRef.value
  if (!el) return
  const wasNearBottom = isNearBottom(
    el.scrollTop,
    el.scrollHeight,
    el.clientHeight,
    STICKY_SCROLL_THRESHOLD,
  )
  if (!wasNearBottom) return
  requestAnimationFrame(() => {
    const current = scrollContainerRef.value
    if (!current) return
    current.scrollTop = current.scrollHeight
  })
}, { flush: 'sync' })

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
  if (!mod || e.key.toLowerCase() !== 'e') return

  e.preventDefault()
  if (e.shiftKey) return
  prefs.mode = prefs.mode === 'edit' ? 'preview' : 'edit'
}

onMounted(() => document.addEventListener('keydown', onKeydown))
onUnmounted(() => {
  document.removeEventListener('keydown', onKeydown)
  flushSave()
})
</script>

<template>
  <div v-if="!activeNotePath" class="flex flex-1 items-center justify-center text-[var(--color-text-dimmed)]">
    <div class="text-center">
      <svg class="mx-auto mb-3 h-12 w-12 opacity-30" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1">
        <path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z" />
        <polyline points="14,2 14,8 20,8" />
      </svg>
      <p class="text-sm">Select a note from the sidebar to begin editing</p>
    </div>
  </div>

  <div v-else class="flex flex-1 flex-col overflow-hidden">
    <!-- Toolbar row -->
    <div class="flex items-center gap-3 border-b border-[var(--color-border-primary)] px-4 py-2">
      <div class="flex-1">
        <JournalToolbar
          :mode="prefs.mode"
          :enhanced="prefs.enhanced"
          :font-family="prefs.fontFamily"
          :font-size="prefs.fontSize"
          :auto-journal-events="currentCampaign?.autoJournalEvents"
          :auto-journal-dice-rolls="currentCampaign?.autoJournalDiceRolls"
          @update:mode="prefs.mode = $event"
          @update:enhanced="prefs.enhanced = $event"
          @update:font-family="prefs.fontFamily = $event"
          @update:font-size="prefs.fontSize = $event"
          @update:auto-journal-events="updateJournalPref('autoJournalEvents', $event)"
          @update:auto-journal-dice-rolls="updateJournalPref('autoJournalDiceRolls', $event)"
        />
      </div>
      <span class="shrink-0 text-xs transition-colors" :class="statusClass">{{ statusText }}</span>
    </div>

    <!-- Editor area -->
    <div class="flex-1 overflow-hidden px-4 py-3">
      <!-- Edit mode -->
      <textarea
        v-if="prefs.mode === 'edit'"
        ref="textareaRef"
        v-model="activeNoteContent"
        :aria-label="`Edit ${activeNoteFileName}`"
        class="h-full w-full resize-none rounded-2xl border border-[var(--color-border-primary)] bg-[var(--color-bg-input)] p-4 leading-relaxed text-[var(--color-text-primary)] shadow-sm outline-none transition placeholder:text-[var(--color-text-dimmed)] focus:border-[var(--color-text-dimmed)] focus:shadow"
        :style="fontStyle"
        placeholder="Start writing..."
        @blur="flushSave"
      />

      <!-- Preview / WYSIWYG mode -->
      <div
        v-else
        ref="scrollContainerRef"
        class="flex h-full flex-col overflow-y-auto rounded-2xl border border-[var(--color-border-primary)] bg-[var(--color-bg-input)] p-4 shadow-sm outline-none transition focus-within:border-[var(--color-text-dimmed)] focus-within:shadow"
        :style="fontStyle"
        @click.self="wysiwygRef?.focusEnd()"
      >
        <WysiwygEditor
          ref="wysiwygRef"
          :content="activeNoteContent"
          :content-key="activeNotePath ?? ''"
          :font-style="fontStyle"
          :disabled="!activeNotePath"
          :enhanced="prefs.enhanced"
          placeholder="Start writing..."
          :all-paths="allPaths"
          :aria-label="`Edit ${activeNoteFileName}`"
          @update:content="activeNoteContent = $event"
          @navigate="handleNavigate"
        />
      </div>
    </div>

    <!-- Wiki-link autocomplete -->
    <WikiLinkAutocomplete
      v-if="activeNotePath && prefs.mode === 'edit'"
      :all-paths="allPaths"
      :textarea="textareaRef"
      :model-value="activeNoteContent"
      @update:model-value="activeNoteContent = $event"
    />
  </div>
</template>
