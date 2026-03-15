<script setup lang="ts">
import { ref, reactive, computed, onMounted, onUnmounted } from 'vue'
import JournalToolbar from '../journal/JournalToolbar.vue'
import JournalPreview from '../journal/JournalPreview.vue'
import WikiLinkAutocomplete from './WikiLinkAutocomplete.vue'
import { useJournalParser } from '../../composables/useJournalParser'
import { useJournalPrefs, FONT_FAMILIES } from '../../composables/useJournalPrefs'
import { useNotes } from '../../composables/useNotes'
import { useCampaign } from '../../composables/useCampaign'
import { apiSend } from '../../composables/useApi'

const props = defineProps<{
  apiOnline: boolean
}>()

const textareaRef = ref<HTMLTextAreaElement | null>(null)
const splitTextareaRef = ref<HTMLTextAreaElement | null>(null)

const { activeNotePath, activeNoteContent, activeNoteFileName, saveStatus, allPaths, openNote, flushSave } = useNotes()
const { currentCampaign, refreshState } = useCampaign()

async function updateJournalPref(key: 'autoJournalEvents' | 'autoJournalDiceRolls', value: boolean) {
  await apiSend('/api/campaigns/journal-prefs', 'PUT', { [key]: value })
  await refreshState()
}

/** Navigates to a wiki-linked note by opening it in the editor. */
async function handleNavigate(path: string) {
  await openNote(path)
}

// Current textarea (split vs single)
const currentTextarea = computed(() => splitTextareaRef.value ?? textareaRef.value)
const { prefs } = useJournalPrefs()
const { segments, deleteSegment } = useJournalParser(activeNoteContent)

// Collapse state for roll panels (must be reactive for Vue to detect Set mutations)
const collapsedIds = reactive(new Set<string>())

/** Toggles the collapsed/expanded state of a roll panel segment. */
function toggleCollapse(id: string) {
  if (collapsedIds.has(id)) collapsedIds.delete(id)
  else collapsedIds.add(id)
}

/** Collapses all roll panel segments into compact view. */
function collapseAll() {
  for (const seg of segments.value) {
    if (seg.type === 'roll') collapsedIds.add(seg.id)
  }
}

/** Expands all roll panel segments to show full details. */
function expandAll() {
  collapsedIds.clear()
}

/** Deletes a roll panel segment from the note content and cleans up its collapse state. */
function handleDelete(id: string) {
  deleteSegment(id)
  collapsedIds.delete(id)
}

const fontStyle = computed(() => ({
  fontFamily: FONT_FAMILIES[prefs.fontFamily] ?? FONT_FAMILIES.mono,
  fontSize: `${prefs.fontSize}px`,
}))

const showPreview = computed(() => prefs.mode === 'preview' || prefs.split)
const showCollapseControls = computed(() => showPreview.value && prefs.enhanced)

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
  if (e.shiftKey) {
    prefs.split = !prefs.split
  } else {
    prefs.split = false
    prefs.mode = prefs.mode === 'edit' ? 'preview' : 'edit'
  }
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
    <div class="flex items-center gap-3 px-4 pt-3">
      <div class="flex-1">
        <JournalToolbar
          :mode="prefs.mode"
          :split="prefs.split"
          :enhanced="prefs.enhanced"
          :font-family="prefs.fontFamily"
          :font-size="prefs.fontSize"
          :show-collapse-controls="showCollapseControls"
          :auto-journal-events="currentCampaign?.autoJournalEvents"
          :auto-journal-dice-rolls="currentCampaign?.autoJournalDiceRolls"
          @update:mode="prefs.mode = $event"
          @update:split="prefs.split = $event"
          @update:enhanced="prefs.enhanced = $event"
          @update:font-family="prefs.fontFamily = $event"
          @update:font-size="prefs.fontSize = $event"
          @update:auto-journal-events="updateJournalPref('autoJournalEvents', $event)"
          @update:auto-journal-dice-rolls="updateJournalPref('autoJournalDiceRolls', $event)"
          @collapse-all="collapseAll"
          @expand-all="expandAll"
        />
      </div>
      <span class="shrink-0 text-xs transition-colors" :class="statusClass">{{ statusText }}</span>
    </div>

    <!-- Editor area -->
    <div class="flex-1 overflow-hidden px-4 pb-4">
      <!-- Split view -->
      <div v-if="prefs.split" class="flex h-full gap-3">
        <textarea
          ref="splitTextareaRef"
          v-model="activeNoteContent"
          :aria-label="`Edit ${activeNoteFileName}`"
          class="h-full flex-1 resize-none rounded-2xl border border-[var(--color-border-primary)] bg-[var(--color-bg-input)] p-4 leading-relaxed text-[var(--color-text-primary)] shadow-sm outline-none transition placeholder:text-[var(--color-text-dimmed)] focus:border-[var(--color-text-dimmed)] focus:shadow"
          :style="fontStyle"
          placeholder="Start writing..."
          @blur="flushSave"
        />
        <div
          class="h-full flex-1 overflow-y-auto rounded-2xl border border-[var(--color-border-primary)] bg-[var(--color-bg-input)] p-4 shadow-sm"
          :style="fontStyle"
        >
          <JournalPreview
            :content="activeNoteContent"
            :enhanced="prefs.enhanced"
            :segments="segments"
            :collapsed-ids="collapsedIds"
            empty-message="Start writing to see preview."
            @toggle="toggleCollapse"
            @delete="handleDelete"
            @navigate="handleNavigate"
          />
        </div>
      </div>

      <!-- Edit mode -->
      <textarea
        v-else-if="prefs.mode === 'edit'"
        ref="textareaRef"
        v-model="activeNoteContent"
        :aria-label="`Edit ${activeNoteFileName}`"
        class="h-full w-full resize-none rounded-2xl border border-[var(--color-border-primary)] bg-[var(--color-bg-input)] p-4 leading-relaxed text-[var(--color-text-primary)] shadow-sm outline-none transition placeholder:text-[var(--color-text-dimmed)] focus:border-[var(--color-text-dimmed)] focus:shadow"
        :style="fontStyle"
        placeholder="Start writing..."
        @blur="flushSave"
      />

      <!-- Preview mode -->
      <div
        v-else
        class="h-full overflow-y-auto rounded-2xl border border-[var(--color-border-primary)] bg-[var(--color-bg-input)] p-4 shadow-sm"
        :style="fontStyle"
      >
        <JournalPreview
          :content="activeNoteContent"
          :enhanced="prefs.enhanced"
          :segments="segments"
          :collapsed-ids="collapsedIds"
          empty-message="Start writing to see preview."
          @toggle="toggleCollapse"
          @delete="handleDelete"
          @navigate="handleNavigate"
        />
      </div>
    </div>

    <!-- Wiki-link autocomplete -->
    <WikiLinkAutocomplete
      v-if="activeNotePath && (prefs.mode === 'edit' || prefs.split)"
      :all-paths="allPaths"
      :textarea="currentTextarea"
      :model-value="activeNoteContent"
      @update:model-value="activeNoteContent = $event"
    />
  </div>
</template>
