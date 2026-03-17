<script setup lang="ts">
import { ref, watch } from 'vue'
import NotesSidebar from '../components/notes/NotesSidebar.vue'
import NoteTabBar from '../components/notes/NoteTabBar.vue'
import NoteEditor from '../components/notes/NoteEditor.vue'
import ToolbarStrip from '../components/journal/ToolbarStrip.vue'
import ToolbarModal from '../components/journal/ToolbarModal.vue'
import CombatRightPanel from '../components/combat/CombatRightPanel.vue'
import { useNotes } from '../composables/useNotes'

defineProps<{
  campaignId: string | null
  apiOnline: boolean
}>()

const notes = useNotes()

const renameTarget = ref<string | null>(null)
const renameValue = ref('')
const activeModalTool = ref<string | null>(null)

// Combat panel state
const combatPanelOpen = ref(loadPanelOpen())
const combatPanelWidth = ref(loadPanelWidth())
const sidebarWasOpen = ref(false)

function loadPanelOpen(): boolean {
  try {
    return localStorage.getItem('soloforge-journal-combat-panel-open') === 'true'
  } catch { /* ignore */ }
  return false
}

function loadPanelWidth(): number {
  try {
    const stored = localStorage.getItem('soloforge-journal-combat-panel-width')
    if (stored) {
      const num = Number(stored)
      if (Number.isFinite(num)) return Math.max(280, Math.min(600, num))
    }
  } catch { /* ignore */ }
  return 400
}

watch(combatPanelOpen, (val) => {
  localStorage.setItem('soloforge-journal-combat-panel-open', String(val))
})

watch(combatPanelWidth, (val) => {
  localStorage.setItem('soloforge-journal-combat-panel-width', String(val))
})

// Auto-collapse sidebar when combat panel is open
if (combatPanelOpen.value) {
  sidebarWasOpen.value = notes.sidebarOpen.value
  notes.sidebarOpen.value = false
}

watch(combatPanelOpen, (open) => {
  if (open) {
    sidebarWasOpen.value = notes.sidebarOpen.value
    notes.sidebarOpen.value = false
  } else {
    notes.sidebarOpen.value = sidebarWasOpen.value
  }
})

/** Opens the rename dialog pre-filled with the current name. */
function startRename(path: string) {
  renameTarget.value = path
  const filename = path.split('/').pop() ?? ''
  renameValue.value = filename.endsWith('.md') ? filename.slice(0, -3) : filename
}

/** Applies the rename by moving the note/folder to the new path. */
async function confirmRename() {
  if (!renameTarget.value || !renameValue.value.trim()) {
    cancelRename()
    return
  }
  const oldPath = renameTarget.value
  const parts = oldPath.split('/')
  const isNote = oldPath.endsWith('.md')
  let newName = renameValue.value.trim().replace(/\//g, '-')
  // Strip trailing .md to avoid double extensions
  if (newName.endsWith('.md')) newName = newName.slice(0, -3)
  if (!newName) {
    cancelRename()
    return
  }
  parts[parts.length - 1] = isNote ? `${newName}.md` : newName
  const newPath = parts.join('/')
  if (newPath !== oldPath) {
    await notes.moveItem(oldPath, newPath)
  }
  cancelRename()
}

/** Closes the rename dialog without making changes. */
function cancelRename() {
  renameTarget.value = null
  renameValue.value = ''
}

async function handleCreateNote(path: string) {
  await notes.createNote(path)
}

async function handleCreateFolder(path: string) {
  await notes.createFolder(path)
}

async function handleDeleteNote(path: string) {
  await notes.deleteNote(path)
}

async function handleDeleteFolder(path: string) {
  await notes.deleteFolder(path)
}

async function handleSetSessionLog(path: string) {
  await notes.setSessionLog(path)
}
</script>

<template>
  <div class="relative flex h-[calc(100vh-10rem)] min-h-[500px] overflow-hidden rounded-3xl border border-[var(--color-border-secondary)] bg-[var(--color-bg-card)] shadow-sm backdrop-blur">
    <!-- Sidebar -->
    <NotesSidebar
      :tree="notes.tree.value"
      :active-path="notes.activeNotePath.value"
      :session-log-path="notes.sessionLogPath.value"
      :open="notes.sidebarOpen.value"
      :loading="notes.loading.tree"
      @update:open="notes.sidebarOpen.value = $event"
      @select="notes.openNote($event)"
      @create-note="handleCreateNote"
      @create-folder="handleCreateFolder"
      @delete-note="handleDeleteNote"
      @delete-folder="handleDeleteFolder"
      @rename="startRename"
      @set-session-log="handleSetSessionLog"
    />

    <!-- Main content area -->
    <div class="flex flex-1 flex-col overflow-hidden pl-1">
      <!-- Tab bar with combat toggle -->
      <div class="flex items-center">
        <NoteTabBar
          class="min-w-0 flex-1"
          :tabs="notes.openTabs.value"
          :active-tab="notes.activeNotePath.value"
          :session-log-path="notes.sessionLogPath.value"
          @select="notes.openNote($event)"
          @close="notes.closeTab($event)"
        />
        <button
          type="button"
          class="mx-1 shrink-0 rounded-lg p-1.5 transition"
          :class="combatPanelOpen
            ? 'bg-[var(--color-bg-accent)] text-[var(--color-text-inverted)]'
            : 'text-[var(--color-text-dimmed)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)]'"
          :aria-label="combatPanelOpen ? 'Close combat tracker' : 'Open combat tracker'"
          :aria-pressed="combatPanelOpen"
          @click="combatPanelOpen = !combatPanelOpen"
        >
          <!-- Sword and shield icon -->
          <svg class="h-5 w-5" viewBox="0 0 24 24" fill="currentColor">
            <!-- Shield -->
            <path d="M5 3.5C5 3.5 8.5 2.5 10 2.5c1.5 0 5 1 5 1v8c0 3.5-5 6.5-5 6.5s-5-3-5-6.5v-8z" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linejoin="round" />
            <!-- Sword -->
            <path d="M16 2l2 2-7 7-2-2 7-7zm-7 7l-1.5 1.5M8.5 10.5L7 12m1.5-1.5L7 9m3 3l-1.5 1.5" stroke="currentColor" stroke-width="1.3" stroke-linecap="round" fill="none" />
          </svg>
        </button>
      </div>

      <!-- Pinned tools toolbar -->
      <ToolbarStrip @open-modal="activeModalTool = $event" />

      <!-- Editor -->
      <NoteEditor :api-online="apiOnline" />
    </div>

    <!-- Combat right panel -->
    <CombatRightPanel
      v-if="combatPanelOpen"
      :width="combatPanelWidth"
      @update:width="combatPanelWidth = $event"
      @close="combatPanelOpen = false"
    />

    <!-- Toolbar tool modal (overlay) -->
    <ToolbarModal :tool-id="activeModalTool" @close="activeModalTool = null" />

    <!-- Rename dialog (overlay) -->
    <div
      v-if="renameTarget"
      class="absolute inset-0 z-50 flex items-center justify-center bg-black/30 backdrop-blur-sm"
      @click.self="cancelRename"
      @keydown.escape="cancelRename"
    >
      <div class="w-80 rounded-2xl border border-[var(--color-border-primary)] bg-[var(--color-bg-card-solid)] p-5 shadow-xl">
        <h3 class="mb-3 text-sm font-semibold text-[var(--color-text-primary)]">Rename</h3>
        <input
          v-model="renameValue"
          class="w-full rounded-xl border border-[var(--color-border-primary)] bg-[var(--color-bg-input)] px-3 py-2 text-sm text-[var(--color-text-primary)] outline-none focus:border-[var(--color-text-dimmed)]"
          aria-label="New name"
          autofocus
          @keydown.enter="confirmRename"
          @keydown.escape="cancelRename"
        />
        <div class="mt-3 flex justify-end gap-2">
          <button
            class="rounded-xl px-3 py-1.5 text-sm text-[var(--color-text-muted)] hover:bg-[var(--color-bg-hover)] transition"
            @click="cancelRename"
          >
            Cancel
          </button>
          <button
            class="rounded-xl bg-[var(--color-bg-accent)] px-3 py-1.5 text-sm font-medium text-[var(--color-text-inverted)] hover:bg-[var(--color-bg-accent-hover)] transition"
            @click="confirmRename"
          >
            Rename
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
