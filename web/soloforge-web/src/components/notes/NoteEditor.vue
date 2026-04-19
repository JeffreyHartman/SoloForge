<script setup lang="ts">
import { ref, computed, nextTick, onActivated, onDeactivated, watch } from 'vue'
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
// Saved scroll positions for when the component is deactivated/reactivated by KeepAlive.
// onDeactivated fires after the DOM is detached (scrollTop already reset to 0),
// so we continuously track scroll via a scroll event listener instead.
let savedTextareaScrollTop = 0
let savedWysiwygScrollTop = 0

function onTextareaScroll() {
  if (textareaRef.value) savedTextareaScrollTop = textareaRef.value.scrollTop
}
function onWysiwygScroll() {
  if (scrollContainerRef.value) savedWysiwygScrollTop = scrollContainerRef.value.scrollTop
}
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
// the bottom before the change. rAF schedules the scroll after Tiptap and Vue
// have both rendered.
//
// flush: 'sync' is load-bearing. It fires this watcher before Vue propagates
// the new content to WysiwygEditor's prop watcher, so scrollHeight still
// reflects the PRE-append document height when wasNearBottom is computed.
// Changing to 'post' or the default flush would read the POST-append height
// and break the "was near bottom" check.
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

// Track textarea scroll position continuously via event listener.
// We cannot read it in onDeactivated because the DOM is already detached by
// then and the browser has reset scrollTop to 0.
watch(textareaRef, (newEl, oldEl) => {
  if (oldEl) oldEl.removeEventListener('scroll', onTextareaScroll)
  if (newEl) {
    newEl.addEventListener('scroll', onTextareaScroll, { passive: true })
    // Restore on re-attach (e.g., after KeepAlive reactivation)
    newEl.scrollTop = savedTextareaScrollTop
  }
})

watch(scrollContainerRef, (newEl, oldEl) => {
  if (oldEl) oldEl.removeEventListener('scroll', onWysiwygScroll)
  if (newEl) {
    newEl.addEventListener('scroll', onWysiwygScroll, { passive: true })
    newEl.scrollTop = savedWysiwygScrollTop
  }
})

// Under <KeepAlive>, onActivated/onDeactivated fire on nav in/out while the
// component remains cached. onActivated also fires right after onMounted on
// the first visit, and onDeactivated fires right before onUnmounted on final
// teardown. Using these hooks for the keydown listener means Ctrl+E only
// toggles Journal mode while Journal is the active view.
//
// Scroll positions are tracked via scroll event listeners (above) because
// onDeactivated fires after DOM detachment, when scrollTop is already 0.
// On reactivation, the watch(textareaRef) callback fires and restores scrollTop.
onActivated(() => {
  document.addEventListener('keydown', onKeydown)
  // After reactivation the refs may still point to the cached elements.
  // Restore scroll via nextTick + rAF to run after Vue's DOM patch.
  const taScroll = savedTextareaScrollTop
  const wsScroll = savedWysiwygScrollTop
  void nextTick(() => {
    requestAnimationFrame(() => {
      if (textareaRef.value) textareaRef.value.scrollTop = taScroll
      if (scrollContainerRef.value) scrollContainerRef.value.scrollTop = wsScroll
    })
  })
})
onDeactivated(() => {
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
