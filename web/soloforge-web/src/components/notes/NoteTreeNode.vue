<script setup lang="ts">
import { ref, computed } from 'vue'
import type { NoteNode } from '../../types'

const props = defineProps<{
  node: NoteNode
  activePath: string | null
  sessionLogPath: string
  depth?: number
}>()

defineEmits<{
  select: [path: string]
  createNote: [parentFolder: string]
  createFolder: [parentFolder: string]
  deleteNote: [path: string]
  deleteFolder: [path: string]
  rename: [path: string]
  setSessionLog: [path: string]
}>()

const expanded = ref(true)
const showContextMenu = ref(false)

const isSessionLog = computed(() => props.node.path === props.sessionLogPath)
const indent = (props.depth ?? 0) * 16
</script>

<template>
  <div>
    <!-- Folder node -->
    <div
      v-if="node.isFolder"
      class="group/node relative flex items-center gap-1 rounded-md px-2 py-1 text-sm cursor-pointer select-none transition-colors hover:bg-[var(--color-bg-hover)]"
      :style="{ paddingLeft: `${indent + 8}px` }"
      role="treeitem"
      :aria-expanded="expanded"
      tabindex="0"
      @click="expanded = !expanded"
      @keydown.enter.prevent="expanded = !expanded"
      @keydown.space.prevent="expanded = !expanded"
      @contextmenu.prevent="showContextMenu = !showContextMenu"
    >
      <!-- Chevron -->
      <svg
        class="h-3.5 w-3.5 shrink-0 text-[var(--color-text-dimmed)] transition-transform"
        :class="{ 'rotate-90': expanded }"
        viewBox="0 0 16 16" fill="currentColor"
      >
        <path d="M6 4l4 4-4 4" />
      </svg>
      <!-- Folder icon -->
      <svg class="h-4 w-4 shrink-0 text-[var(--color-text-warning)]" viewBox="0 0 20 20" fill="currentColor">
        <path d="M2 6a2 2 0 012-2h5l2 2h5a2 2 0 012 2v6a2 2 0 01-2 2H4a2 2 0 01-2-2V6z" />
      </svg>
      <span class="truncate text-[var(--color-text-primary)]">{{ node.name }}</span>

      <!-- Folder actions (hover) -->
      <div class="ml-auto flex items-center gap-0.5 opacity-0 group-hover/node:opacity-100 transition-opacity">
        <button
          class="rounded p-0.5 text-[var(--color-text-dimmed)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)] transition"
          title="New note"
          aria-label="New note in folder"
          @click.stop="$emit('createNote', node.path)"
        >
          <svg class="h-3.5 w-3.5" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M8 3v10M3 8h10" />
          </svg>
        </button>
        <button
          class="rounded p-0.5 text-[var(--color-text-dimmed)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)] transition"
          title="New subfolder"
          aria-label="New subfolder"
          @click.stop="$emit('createFolder', node.path)"
        >
          <svg class="h-3.5 w-3.5" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5">
            <path d="M2 5a1.5 1.5 0 011.5-1.5H6l1.5 1.5H12.5A1.5 1.5 0 0114 6.5V11a1.5 1.5 0 01-1.5 1.5h-9A1.5 1.5 0 012 11V5z" />
            <path d="M8 7v4M6 9h4" />
          </svg>
        </button>
        <button
          class="rounded p-0.5 text-[var(--color-text-dimmed)] hover:text-[var(--color-text-danger)] hover:bg-[var(--color-bg-danger)] transition"
          title="Delete folder"
          aria-label="Delete folder"
          @click.stop="$emit('deleteFolder', node.path)"
        >
          <svg class="h-3.5 w-3.5" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M4 4l8 8M12 4l-8 8" />
          </svg>
        </button>
      </div>
    </div>

    <!-- Folder children -->
    <div v-if="node.isFolder && expanded" role="group">
      <NoteTreeNode
        v-for="child in node.children"
        :key="child.path"
        :node="child"
        :active-path="activePath"
        :session-log-path="sessionLogPath"
        :depth="(depth ?? 0) + 1"
        @select="$emit('select', $event)"
        @create-note="$emit('createNote', $event)"
        @create-folder="$emit('createFolder', $event)"
        @delete-note="$emit('deleteNote', $event)"
        @delete-folder="$emit('deleteFolder', $event)"
        @rename="$emit('rename', $event)"
        @set-session-log="$emit('setSessionLog', $event)"
      />
    </div>

    <!-- File node -->
    <div
      v-if="!node.isFolder"
      class="group/node relative flex items-center gap-1.5 rounded-md px-2 py-1 text-sm cursor-pointer select-none transition-colors"
      :class="activePath === node.path
        ? 'bg-[var(--color-bg-accent)] text-[var(--color-text-inverted)]'
        : 'text-[var(--color-text-secondary)] hover:bg-[var(--color-bg-hover)]'"
      :style="{ paddingLeft: `${indent + 24}px` }"
      role="treeitem"
      :aria-selected="activePath === node.path"
      tabindex="0"
      @click="$emit('select', node.path)"
      @keydown.enter.prevent="$emit('select', node.path)"
      @keydown.space.prevent="$emit('select', node.path)"
    >
      <!-- Document icon -->
      <svg class="h-4 w-4 shrink-0 opacity-60" viewBox="0 0 20 20" fill="currentColor">
        <path fill-rule="evenodd" d="M4 4a2 2 0 012-2h4.586A2 2 0 0112 2.586L15.414 6A2 2 0 0116 7.414V16a2 2 0 01-2 2H6a2 2 0 01-2-2V4z" clip-rule="evenodd" />
      </svg>
      <span class="truncate">{{ node.name }}</span>

      <!-- Session log badge -->
      <span
        v-if="isSessionLog"
        class="ml-1 shrink-0 rounded px-1 py-0.5 text-[9px] font-bold uppercase tracking-wider"
        :class="activePath === node.path
          ? 'bg-white/20 text-[var(--color-text-inverted)]'
          : 'bg-[var(--color-bg-info)] text-[var(--color-text-info)]'"
      >
        Log
      </span>

      <!-- File actions (hover) -->
      <div
        class="ml-auto flex items-center gap-0.5 transition-opacity"
        :class="activePath === node.path ? 'opacity-70 hover:opacity-100' : 'opacity-0 group-hover/node:opacity-100'"
      >
        <button
          v-if="!isSessionLog"
          class="rounded p-0.5 transition"
          :class="activePath === node.path
            ? 'text-[var(--color-text-inverted)] hover:bg-white/20'
            : 'text-[var(--color-text-dimmed)] hover:text-[var(--color-text-info)] hover:bg-[var(--color-bg-info)]'"
          title="Set as session log"
          aria-label="Set as session log"
          @click.stop="$emit('setSessionLog', node.path)"
        >
          <svg class="h-3.5 w-3.5" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5">
            <path d="M3 3h10v10H3zM6 1v4M10 1v4" />
          </svg>
        </button>
        <button
          class="rounded p-0.5 transition"
          :class="activePath === node.path
            ? 'text-[var(--color-text-inverted)] hover:bg-white/20'
            : 'text-[var(--color-text-dimmed)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)]'"
          title="Rename"
          aria-label="Rename note"
          @click.stop="$emit('rename', node.path)"
        >
          <svg class="h-3.5 w-3.5" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5">
            <path d="M11.5 1.5l3 3-9 9H2.5v-3l9-9z" />
          </svg>
        </button>
        <button
          class="rounded p-0.5 transition"
          :class="activePath === node.path
            ? 'text-[var(--color-text-inverted)] hover:bg-red-500/30'
            : 'text-[var(--color-text-dimmed)] hover:text-[var(--color-text-danger)] hover:bg-[var(--color-bg-danger)]'"
          title="Delete note"
          aria-label="Delete note"
          @click.stop="$emit('deleteNote', node.path)"
        >
          <svg class="h-3.5 w-3.5" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M4 4l8 8M12 4l-8 8" />
          </svg>
        </button>
      </div>
    </div>
  </div>
</template>
