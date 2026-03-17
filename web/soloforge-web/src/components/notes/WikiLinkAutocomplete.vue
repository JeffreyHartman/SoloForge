<script setup lang="ts">
import { ref, computed, watch, nextTick, onMounted, onUnmounted } from 'vue'

const props = defineProps<{
  allPaths: string[]
  textarea: HTMLTextAreaElement | null
  modelValue: string
}>()

const emit = defineEmits<{
  'update:modelValue': [value: string]
}>()

const show = ref(false)
const query = ref('')
const selectedIndex = ref(0)
const dropdownStyle = ref<Record<string, string>>({})

// Mirror element for cursor position calculation
let mirror: HTMLDivElement | null = null

/** Extracts the display name from a note path, stripping folders and .md extension. */
function pathToName(path: string): string {
  const filename = path.split('/').pop() ?? path
  return filename.endsWith('.md') ? filename.slice(0, -3) : filename
}

const filtered = computed(() => {
  const q = query.value.toLowerCase()
  if (!q) {
    return props.allPaths.slice(0, 15).map(p => ({ path: p, name: pathToName(p) }))
  }
  return props.allPaths
    .map(p => ({ path: p, name: pathToName(p) }))
    .filter(item => item.name.toLowerCase().includes(q) || item.path.toLowerCase().includes(q))
    .sort((a, b) => {
      // Prioritize starts-with matches
      const aStarts = a.name.toLowerCase().startsWith(q) ? 0 : 1
      const bStarts = b.name.toLowerCase().startsWith(q) ? 0 : 1
      if (aStarts !== bStarts) return aStarts - bStarts
      return a.name.localeCompare(b.name)
    })
    .slice(0, 15)
})

watch(filtered, () => {
  selectedIndex.value = 0
})

/** Calculates pixel coordinates for a caret position in a textarea using a hidden mirror div. */
function getCaretCoords(textarea: HTMLTextAreaElement, position: number) {
  if (!mirror) {
    mirror = document.createElement('div')
    document.body.appendChild(mirror)
  }

  const style = window.getComputedStyle(textarea)
  const properties = [
    'fontFamily', 'fontSize', 'fontWeight', 'fontStyle',
    'letterSpacing', 'textTransform', 'wordSpacing',
    'textIndent', 'padding', 'paddingTop', 'paddingRight',
    'paddingBottom', 'paddingLeft', 'borderWidth', 'boxSizing',
    'lineHeight', 'whiteSpace', 'wordWrap', 'overflowWrap',
  ] as const

  mirror.style.position = 'absolute'
  mirror.style.visibility = 'hidden'
  mirror.style.top = '0'
  mirror.style.left = '0'
  mirror.style.width = style.width
  mirror.style.height = 'auto'
  mirror.style.overflow = 'hidden'

  for (const prop of properties) {
    ;(mirror.style as any)[prop] = style.getPropertyValue(
      prop.replace(/[A-Z]/g, c => `-${c.toLowerCase()}`)
    )
  }

  const textBefore = textarea.value.substring(0, position)
  const span = document.createElement('span')
  span.textContent = '|'

  mirror.textContent = textBefore
  mirror.appendChild(span)

  const rect = textarea.getBoundingClientRect()
  const spanRect = span.getBoundingClientRect()

  return {
    top: rect.top + (spanRect.top - mirror.getBoundingClientRect().top) - textarea.scrollTop,
    left: rect.left + (spanRect.left - mirror.getBoundingClientRect().left) - textarea.scrollLeft,
  }
}

/** Checks if the cursor is inside a `[[` trigger and shows/positions the autocomplete dropdown. */
function checkForTrigger() {
  const ta = props.textarea
  if (!ta) return

  const pos = ta.selectionStart
  const text = ta.value
  const before = text.substring(0, pos)

  // Find the last [[ before the cursor
  const triggerIdx = before.lastIndexOf('[[')
  if (triggerIdx === -1) {
    show.value = false
    return
  }

  // Check that there's no ]] between [[ and cursor
  const between = before.substring(triggerIdx + 2)
  if (between.includes(']]') || between.includes('\n')) {
    show.value = false
    return
  }

  query.value = between
  show.value = true

  // Position the dropdown, flipping above if near bottom of screen
  const coords = getCaretCoords(ta, triggerIdx)
  const dropdownHeight = 240 // max-h-60 = 15rem ≈ 240px
  const spaceBelow = window.innerHeight - (coords.top + 24)
  const spaceAbove = coords.top

  if (spaceBelow < dropdownHeight && spaceAbove > spaceBelow) {
    dropdownStyle.value = {
      position: 'fixed',
      bottom: `${window.innerHeight - coords.top + 4}px`,
      left: `${coords.left}px`,
      zIndex: '100',
    }
  } else {
    dropdownStyle.value = {
      position: 'fixed',
      top: `${coords.top + 24}px`,
      left: `${coords.left}px`,
      zIndex: '100',
    }
  }
}

/** Checks if multiple notes share the same basename, requiring full-path disambiguation. */
function isAmbiguous(item: { path: string; name: string }): boolean {
  return props.allPaths.filter(p => pathToName(p) === item.name).length > 1
}

/** Inserts the selected wiki-link at the cursor position, using full path for ambiguous names. */
function insertSelection(item: { path: string; name: string }) {
  const ta = props.textarea
  if (!ta) return

  const pos = ta.selectionStart
  const text = ta.value
  const before = text.substring(0, pos)
  const triggerIdx = before.lastIndexOf('[[')

  if (triggerIdx === -1) return

  // Use full path when basename collides with another note, alias form: [[path|display]]
  const linkText = isAmbiguous(item)
    ? `${item.path}|${item.name}`
    : item.name
  const newText = text.substring(0, triggerIdx) + `[[${linkText}]]` + text.substring(pos)
  emit('update:modelValue', newText)

  show.value = false

  const newPos = triggerIdx + linkText.length + 4 // [[linkText]]
  nextTick(() => {
    ta.focus()
    ta.setSelectionRange(newPos, newPos)
  })
}

/** Handles keyboard navigation (arrows, tab/enter, escape) within the autocomplete dropdown. */
function onKeydown(e: KeyboardEvent) {
  if (!show.value) return

  if (e.key === 'ArrowDown') {
    e.preventDefault()
    selectedIndex.value = Math.min(selectedIndex.value + 1, filtered.value.length - 1)
  } else if (e.key === 'ArrowUp') {
    e.preventDefault()
    selectedIndex.value = Math.max(selectedIndex.value - 1, 0)
  } else if (e.key === 'Tab' || e.key === 'Enter') {
    if (filtered.value.length > 0) {
      e.preventDefault()
      insertSelection(filtered.value[selectedIndex.value]!)
    }
  } else if (e.key === 'Escape') {
    e.preventDefault()
    show.value = false
  }
}

/** Re-checks for the `[[` trigger on each textarea input event. */
function onInput() {
  checkForTrigger()
}

onMounted(() => {
  const ta = props.textarea
  if (ta) {
    ta.addEventListener('input', onInput)
    ta.addEventListener('keydown', onKeydown)
    ta.addEventListener('click', checkForTrigger)
  }
})

onUnmounted(() => {
  const ta = props.textarea
  if (ta) {
    ta.removeEventListener('input', onInput)
    ta.removeEventListener('keydown', onKeydown)
    ta.removeEventListener('click', checkForTrigger)
  }
  if (mirror) {
    mirror.remove()
    mirror = null
  }
})

// Re-attach listeners when textarea changes
watch(() => props.textarea, (newTa, oldTa) => {
  if (oldTa) {
    oldTa.removeEventListener('input', onInput)
    oldTa.removeEventListener('keydown', onKeydown)
    oldTa.removeEventListener('click', checkForTrigger)
  }
  if (newTa) {
    newTa.addEventListener('input', onInput)
    newTa.addEventListener('keydown', onKeydown)
    newTa.addEventListener('click', checkForTrigger)
  }
})
</script>

<template>
  <Teleport to="body">
    <div
      v-if="show && filtered.length > 0"
      :style="dropdownStyle"
      class="max-h-60 w-64 overflow-y-auto rounded-xl border border-[var(--color-border-primary)] bg-[var(--color-bg-card-solid)] py-1 shadow-xl"
      role="listbox"
      aria-label="Note suggestions"
    >
      <div
        v-for="(item, i) in filtered"
        :key="item.path"
        class="flex items-center gap-2 px-3 py-1.5 text-sm cursor-pointer transition-colors"
        :class="i === selectedIndex
          ? 'bg-[var(--color-bg-accent)] text-[var(--color-text-inverted)]'
          : 'text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)]'"
        role="option"
        :aria-selected="i === selectedIndex"
        @mousedown.prevent="insertSelection(item)"
        @mouseenter="selectedIndex = i"
      >
        <svg class="h-3.5 w-3.5 shrink-0 opacity-50" viewBox="0 0 20 20" fill="currentColor">
          <path fill-rule="evenodd" d="M4 4a2 2 0 012-2h4.586A2 2 0 0112 2.586L15.414 6A2 2 0 0116 7.414V16a2 2 0 01-2 2H6a2 2 0 01-2-2V4z" clip-rule="evenodd" />
        </svg>
        <div class="min-w-0 flex-1">
          <div class="truncate font-medium">{{ item.name }}</div>
          <div
            v-if="item.path !== item.name + '.md'"
            class="truncate text-xs"
            :class="i === selectedIndex ? 'opacity-70' : 'text-[var(--color-text-dimmed)]'"
          >
            {{ item.path }}
          </div>
        </div>
      </div>
    </div>
  </Teleport>
</template>
