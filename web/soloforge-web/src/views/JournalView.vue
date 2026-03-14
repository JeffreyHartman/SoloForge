<script setup lang="ts">
import { ref } from 'vue'
import NotesSidebar from '../components/notes/NotesSidebar.vue'
import NoteTabBar from '../components/notes/NoteTabBar.vue'
import NoteEditor from '../components/notes/NoteEditor.vue'
import ToolbarStrip from '../components/journal/ToolbarStrip.vue'
import ToolbarModal from '../components/journal/ToolbarModal.vue'
import { useNotes } from '../composables/useNotes'

defineProps<{
  campaignId: string | null
  apiOnline: boolean
}>()

const notes = useNotes()

const renameTarget = ref<string | null>(null)
const renameValue = ref('')
const activeModalTool = ref<string | null>(null)

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
    <div class="flex flex-1 flex-col overflow-hidden">
      <!-- Tab bar -->
      <NoteTabBar
        :tabs="notes.openTabs.value"
        :active-tab="notes.activeNotePath.value"
        :session-log-path="notes.sessionLogPath.value"
        @select="notes.openNote($event)"
        @close="notes.closeTab($event)"
      />

      <!-- Pinned tools toolbar -->
      <ToolbarStrip @open-modal="activeModalTool = $event" />

      <!-- Editor -->
      <NoteEditor :api-online="apiOnline" />
    </div>

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
