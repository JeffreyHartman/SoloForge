<script setup lang="ts">
import { ref, nextTick, watch } from 'vue'
import NoteTreeNode from './NoteTreeNode.vue'
import type { NoteNode } from '../../types'

const props = defineProps<{
  tree: NoteNode[]
  activePath: string | null
  sessionLogPath: string
  open: boolean
  loading: boolean
}>()

const emit = defineEmits<{
  'update:open': [value: boolean]
  select: [path: string]
  createNote: [parentFolder: string]
  createFolder: [parentFolder: string]
  deleteNote: [path: string]
  deleteFolder: [path: string]
  rename: [path: string]
  setSessionLog: [path: string]
  createRootNote: []
  createRootFolder: []
}>()

const newItemName = ref('')
const newItemType = ref<'note' | 'folder' | null>(null)
const newItemParent = ref<string>('')

const createInputRef = ref<HTMLInputElement | null>(null)

/** Opens the inline create form for a new note, auto-focusing the name input. */
function startCreateNote(parentFolder: string) {
  newItemType.value = 'note'
  newItemParent.value = parentFolder
  newItemName.value = ''
  nextTick(() => createInputRef.value?.focus())
}

/** Opens the inline create form for a new folder, auto-focusing the name input. */
function startCreateFolder(parentFolder: string) {
  newItemType.value = 'folder'
  newItemParent.value = parentFolder
  newItemName.value = ''
  nextTick(() => createInputRef.value?.focus())
}

/** Emits the create event with the full path and closes the inline form. */
function confirmCreate() {
  const name = newItemName.value.trim()
  if (!name) {
    cancelCreate()
    return
  }

  const parent = newItemParent.value
  if (newItemType.value === 'note') {
    const path = parent ? `${parent}/${name}.md` : `${name}.md`
    emit('createNote', path)
  } else if (newItemType.value === 'folder') {
    const path = parent ? `${parent}/${name}` : name
    emit('createFolder', path)
  }
  cancelCreate()
}

/** Closes the inline create form without creating anything. */
function cancelCreate() {
  newItemType.value = null
  newItemName.value = ''
  newItemParent.value = ''
}
</script>

<template>
  <!-- Sidebar area: expanded drawer or collapsed toggle strip -->
  <div
    class="shrink-0 overflow-hidden transition-all duration-300 ease-in-out"
    :style="{ width: open ? '280px' : '36px' }"
  >
    <!-- Collapsed state: just the toggle arrow -->
    <div v-if="!open" class="flex h-full w-[36px] flex-col items-center border-r border-[var(--color-border-primary)] bg-[var(--color-bg-card-solid)] pt-2">
      <button
        class="rounded-lg p-1.5 text-[var(--color-text-dimmed)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)] transition"
        :aria-expanded="false"
        aria-label="Open notes sidebar"
        @click="$emit('update:open', true)"
      >
        <svg class="h-4 w-4" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2">
          <path d="M6 4l4 4-4 4" />
        </svg>
      </button>
    </div>

    <!-- Expanded state: full sidebar -->
    <div v-else class="flex h-full w-[280px] flex-col border-r border-[var(--color-border-primary)] bg-[var(--color-bg-card-solid)]">
      <!-- Header -->
      <div class="flex items-center justify-between border-b border-[var(--color-border-primary)] px-3 py-2.5">
        <span class="text-xs font-semibold uppercase tracking-wider text-[var(--color-text-muted)]">Notes</span>
        <div class="flex items-center gap-1">
          <button
            class="rounded p-1 text-[var(--color-text-dimmed)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)] transition"
            title="New note"
            aria-label="New note"
            @click="startCreateNote('')"
          >
            <svg class="h-4 w-4" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M8 3v10M3 8h10" />
            </svg>
          </button>
          <button
            class="rounded p-1 text-[var(--color-text-dimmed)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)] transition"
            title="New folder"
            aria-label="New folder"
            @click="startCreateFolder('')"
          >
            <svg class="h-4 w-4" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M2 5a1.5 1.5 0 011.5-1.5H6l1.5 1.5H12.5A1.5 1.5 0 0114 6.5V11a1.5 1.5 0 01-1.5 1.5h-9A1.5 1.5 0 012 11V5z" />
              <path d="M8 7v4M6 9h4" />
            </svg>
          </button>
          <button
            class="rounded p-1 text-[var(--color-text-dimmed)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)] transition"
            title="Collapse sidebar"
            aria-label="Collapse notes sidebar"
            :aria-expanded="true"
            @click="$emit('update:open', false)"
          >
            <svg class="h-4 w-4" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M10 4l-4 4 4 4" />
            </svg>
          </button>
        </div>
      </div>

      <!-- Inline create form -->
      <div v-if="newItemType" class="border-b border-[var(--color-border-primary)] px-3 py-2">
        <div class="text-[10px] font-semibold uppercase tracking-wider text-[var(--color-text-muted)] mb-1">
          New {{ newItemType }}{{ newItemParent ? ` in ${newItemParent}` : '' }}
        </div>
        <div class="flex gap-1.5">
          <input
            ref="createInputRef"
            v-model="newItemName"
            :placeholder="newItemType === 'note' ? 'Note name' : 'Folder name'"
            class="flex-1 rounded-lg border border-[var(--color-border-primary)] bg-[var(--color-bg-input)] px-2 py-1 text-sm text-[var(--color-text-primary)] outline-none focus:border-[var(--color-text-dimmed)]"
            aria-label="New item name"
            @keydown.enter="confirmCreate"
            @keydown.escape="cancelCreate"
          />
          <button
            class="rounded-lg bg-[var(--color-bg-accent)] px-2 py-1 text-xs font-medium text-[var(--color-text-inverted)] hover:bg-[var(--color-bg-accent-hover)] transition"
            @click="confirmCreate"
          >
            Add
          </button>
          <button
            class="rounded-lg px-2 py-1 text-xs text-[var(--color-text-muted)] hover:bg-[var(--color-bg-hover)] transition"
            @click="cancelCreate"
          >
            Cancel
          </button>
        </div>
      </div>

      <!-- Tree -->
      <div class="flex-1 overflow-y-auto px-1 py-2" role="tree" aria-label="Notes tree">
        <div v-if="loading" class="flex items-center justify-center py-8">
          <span class="h-5 w-5 animate-spin rounded-full border-2 border-[var(--color-text-dimmed)] border-t-transparent" />
        </div>
        <div v-else-if="tree.length === 0" class="px-3 py-8 text-center text-xs text-[var(--color-text-dimmed)]">
          No notes yet. Create one to get started.
        </div>
        <NoteTreeNode
          v-for="node in tree"
          :key="node.path"
          :node="node"
          :active-path="activePath"
          :session-log-path="sessionLogPath"
          :depth="0"
          @select="$emit('select', $event)"
          @create-note="startCreateNote"
          @create-folder="startCreateFolder"
          @delete-note="$emit('deleteNote', $event)"
          @delete-folder="$emit('deleteFolder', $event)"
          @rename="$emit('rename', $event)"
          @set-session-log="$emit('setSessionLog', $event)"
        />
      </div>
    </div>
  </div>
</template>
