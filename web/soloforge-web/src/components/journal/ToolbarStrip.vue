<script setup lang="ts">
import { ref } from 'vue'
import { useToolbarPrefs } from '../../composables/useToolbarPrefs'
import { useMythic } from '../../composables/useMythic'
import { useToolActions } from '../../composables/useToolActions'
import { useResultBanner, formatRandomResult, formatDiceResult } from '../../composables/useResultBanner'
import { getPinnableTool } from '../../tools/pinnableTools'
import type { ToolbarItem } from '../../composables/useToolbarPrefs'

const emit = defineEmits<{
  'open-modal': [toolId: string]
}>()

const { prefs, moveItem, addSeparator, removeItem, updateSeparatorLabel } = useToolbarPrefs()
const { runRandomEvent, randomResult, rollDice, diceResult } = useMythic()
const { runAction, apiOnline } = useToolActions()
const { showBanner } = useResultBanner()

const editMode = ref(false)
const dragIndex = ref<number | null>(null)
const editingSepIndex = ref<number | null>(null)
const editingSepValue = ref('')

function handleToolClick(toolId: string) {
  const tool = getPinnableTool(toolId)
  if (!tool) return

  if (tool.execution === 'instant') {
    void runInstant(toolId)
  } else {
    emit('open-modal', toolId)
  }
}

async function runInstant(toolId: string) {
  if (toolId === 'random-event') {
    await runAction(async () => {
      await runRandomEvent()
    })
    if (randomResult.value) {
      showBanner(formatRandomResult(randomResult.value))
    }
  }
}

async function handleQuickDice(expression: string) {
  await runAction(async () => {
    await rollDice(expression)
  })
  if (diceResult.value) {
    showBanner(formatDiceResult(diceResult.value))
  }
}

function handleDragStart(index: number) {
  dragIndex.value = index
}

function handleDragOver(e: DragEvent) {
  e.preventDefault()
}

function handleDrop(targetIndex: number) {
  if (dragIndex.value !== null && dragIndex.value !== targetIndex) {
    moveItem(dragIndex.value, targetIndex)
  }
  dragIndex.value = null
}

function handleDragEnd() {
  dragIndex.value = null
}

function handleAddSeparator() {
  addSeparator()
}

function startEditSeparator(index: number, item: ToolbarItem) {
  if (item.type !== 'separator') return
  editingSepIndex.value = index
  editingSepValue.value = item.label ?? ''
}

function commitSeparatorLabel() {
  if (editingSepIndex.value !== null) {
    updateSeparatorLabel(editingSepIndex.value, editingSepValue.value.trim())
  }
  editingSepIndex.value = null
  editingSepValue.value = ''
}
</script>

<template>
  <div
    v-if="prefs.items.length > 0"
    class="flex items-center gap-1 border-b border-[var(--color-border-primary)] bg-[var(--color-bg-card)] px-3 py-1.5"
  >
    <template v-for="(item, index) in prefs.items" :key="index">
      <!-- Separator -->
      <template v-if="item.type === 'separator'">
        <div
          v-if="editMode"
          class="flex items-center gap-1"
          :draggable="true"
          @dragstart="handleDragStart(index)"
          @dragover="handleDragOver"
          @drop="handleDrop(index)"
          @dragend="handleDragEnd"
        >
          <svg class="h-3 w-3 cursor-grab text-[var(--color-text-dimmed)]" viewBox="0 0 12 12" fill="currentColor">
            <circle cx="3" cy="3" r="1.2" /><circle cx="9" cy="3" r="1.2" />
            <circle cx="3" cy="9" r="1.2" /><circle cx="9" cy="9" r="1.2" />
          </svg>
          <template v-if="editingSepIndex === index">
            <input
              v-model="editingSepValue"
              class="w-16 rounded border border-[var(--color-border-primary)] bg-[var(--color-bg-input)] px-1 py-0.5 text-[10px] uppercase tracking-wider text-[var(--color-text-muted)] outline-none"
              aria-label="Separator label"
              autofocus
              @keydown.enter="commitSeparatorLabel"
              @keydown.escape="editingSepIndex = null"
              @blur="commitSeparatorLabel"
            />
          </template>
          <button
            v-else
            type="button"
            class="px-1 text-[10px] font-semibold uppercase tracking-wider text-[var(--color-text-dimmed)] hover:text-[var(--color-text-muted)] transition"
            :aria-label="item.label ? `Edit separator '${item.label}'` : 'Edit separator'"
            @click="startEditSeparator(index, item)"
          >
            {{ item.label || '|' }}
          </button>
          <button
            type="button"
            class="rounded p-0.5 text-[var(--color-text-dimmed)] hover:text-[var(--color-text-danger)] hover:bg-[var(--color-bg-hover)] transition"
            aria-label="Remove separator"
            @click="removeItem(index)"
          >
            <svg class="h-3 w-3" viewBox="0 0 12 12" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M2 2l8 8M10 2l-8 8" />
            </svg>
          </button>
        </div>
        <span v-else-if="item.label" class="select-none px-1 text-[10px] font-semibold uppercase tracking-wider text-[var(--color-text-dimmed)]">
          {{ item.label }}
        </span>
        <span v-else class="select-none px-1 text-[var(--color-text-dimmed)]">|</span>
      </template>

      <!-- Tool button -->
      <template v-else>
        <div
          class="flex items-center gap-1"
          :draggable="editMode"
          @dragstart="editMode && handleDragStart(index)"
          @dragover="editMode && handleDragOver($event)"
          @drop="editMode && handleDrop(index)"
          @dragend="editMode && handleDragEnd()"
        >
          <svg v-if="editMode" class="h-3 w-3 cursor-grab text-[var(--color-text-dimmed)]" viewBox="0 0 12 12" fill="currentColor">
            <circle cx="3" cy="3" r="1.2" /><circle cx="9" cy="3" r="1.2" />
            <circle cx="3" cy="9" r="1.2" /><circle cx="9" cy="9" r="1.2" />
          </svg>

          <!-- Dice roller with expanded sub-actions -->
          <template v-if="item.toolId === 'dice-roller' && getPinnableTool('dice-roller')?.expandsInToolbar">
            <button
              type="button"
              class="rounded-lg px-2 py-1 text-xs font-semibold text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)] transition"
              aria-label="Open Dice Roller"
              @click="emit('open-modal', 'dice-roller')"
            >
              Dice
            </button>
            <button
              v-for="sub in getPinnableTool('dice-roller')?.subActions ?? []"
              :key="sub.id"
              type="button"
              class="rounded-full border border-[var(--color-border-primary)] bg-[var(--color-bg-card-solid)] px-2 py-0.5 text-[11px] font-semibold text-[var(--color-text-primary)] shadow-sm transition hover:bg-[var(--color-bg-hover)]"
              :disabled="!apiOnline"
              :aria-label="`Roll ${sub.expression}`"
              @click="handleQuickDice(sub.expression)"
            >
              {{ sub.label }}
            </button>
          </template>

          <!-- Standard tool button -->
          <button
            v-else
            type="button"
            class="rounded-lg px-2 py-1 text-xs font-semibold text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)] transition"
            :disabled="!apiOnline"
            :aria-label="getPinnableTool(item.toolId)?.label ?? item.toolId"
            @click="handleToolClick(item.toolId)"
          >
            {{ getPinnableTool(item.toolId)?.label ?? item.toolId }}
          </button>

          <button
            v-if="editMode"
            type="button"
            class="rounded p-0.5 text-[var(--color-text-dimmed)] hover:text-[var(--color-text-danger)] hover:bg-[var(--color-bg-hover)] transition"
            aria-label="Remove from toolbar"
            @click="removeItem(index)"
          >
            <svg class="h-3 w-3" viewBox="0 0 12 12" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M2 2l8 8M10 2l-8 8" />
            </svg>
          </button>
        </div>
      </template>
    </template>

    <!-- Spacer -->
    <div class="flex-1" />

    <!-- Edit-mode: add separator button -->
    <button
      v-if="editMode"
      type="button"
      class="rounded-lg px-2 py-1 text-[11px] font-medium text-[var(--color-text-dimmed)] hover:bg-[var(--color-bg-hover)] hover:text-[var(--color-text-muted)] transition"
      aria-label="Add separator"
      @click="handleAddSeparator"
    >
      + separator
    </button>

    <!-- Edit mode toggle -->
    <button
      type="button"
      class="rounded-lg p-1.5 transition"
      :class="editMode ? 'bg-[var(--color-bg-accent)] text-[var(--color-text-inverted)]' : 'text-[var(--color-text-dimmed)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)]'"
      :aria-label="editMode ? 'Exit edit mode' : 'Edit toolbar'"
      :aria-pressed="editMode"
      @click="editMode = !editMode"
    >
      <svg class="h-3.5 w-3.5" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5">
        <path d="M11.5 1.5l3 3-9 9H2.5v-3l9-9z" />
        <path d="M9.5 3.5l3 3" />
      </svg>
    </button>
  </div>
</template>
