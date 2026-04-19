<script setup lang="ts">
import { ref, watch, onBeforeUnmount, nextTick } from 'vue'
import { Selection } from '@tiptap/pm/state'
import { useEditor, EditorContent } from '@tiptap/vue-3'
import StarterKit from '@tiptap/starter-kit'
import { Table, TableRow, TableCell, TableHeader } from '@tiptap/extension-table'
import Placeholder from '@tiptap/extension-placeholder'
import { Markdown } from '@tiptap/markdown'
import { RollTableNode } from './tiptap/RollTableNode'
import { NoteBlockNode } from './tiptap/NoteBlockNode'
import { WikiLinkMark } from './tiptap/WikiLinkMark'
import { createWikiLinkSuggestion } from './tiptap/wikiLinkSuggestion'

const props = withDefaults(defineProps<{
  content: string | undefined
  fontStyle: Record<string, string | undefined>
  disabled: boolean
  placeholder: string
  allPaths?: string[]
  enhanced?: boolean
}>(), {
  enhanced: true,
})

const emit = defineEmits<{
  'update:content': [value: string]
  navigate: [path: string]
}>()

const isUpdatingFromProp = ref(false)

const editor = useEditor({
  content: props.content ?? '',
  contentType: 'markdown',
  editable: !props.disabled,
  extensions: [
    StarterKit,
    Table.configure({ resizable: false }),
    TableRow,
    TableCell,
    TableHeader,
    Placeholder.configure({ placeholder: props.placeholder }),
    Markdown.configure({
      markedOptions: { gfm: true, breaks: true },
    }),
    RollTableNode,
    NoteBlockNode,
    WikiLinkMark,
    createWikiLinkSuggestion({
      allPaths: () => props.allPaths ?? [],
    }),
  ],
  onCreate({ editor: ed }) {
    const storage = ed.storage as any
    if (storage.rollTable) storage.rollTable.enhanced = props.enhanced
    if (storage.noteBlock) storage.noteBlock.enhanced = props.enhanced
  },
  onUpdate({ editor: ed }) {
    if (isUpdatingFromProp.value) return
    const md = ed.getMarkdown()
    emit('update:content', md)
  },
  editorProps: {
    handleClickOn(_view, _pos, _node, _nodePos, event) {
      const target = (event.target as HTMLElement).closest('.wiki-link') as HTMLElement | null
      if (target?.dataset.path) {
        event.preventDefault()
        emit('navigate', target.dataset.path)
        return true
      }
      return false
    },
    handleClick(view, _pos, event) {
      // If the click landed directly on the ProseMirror root (empty space below content),
      // move the cursor to the end of the document — mimics textarea behavior.
      if (event.target === view.dom) {
        const end = view.state.doc.content.size
        view.dispatch(view.state.tr.setSelection(
          Selection.near(view.state.doc.resolve(end), -1)
        ))
        view.focus()
        return true
      }
      return false
    },
    handleKeyDown(view, event) {
      if (event.key === 'Enter') {
        // Check if the cursor is inside a wiki-link mark
        const { $from } = view.state.selection
        const marks = $from.marks()
        const wikiMark = marks.find(m => m.type.name === 'wikiLink')
        if (wikiMark) {
          event.preventDefault()
          emit('navigate', wikiMark.attrs.path)
          return true
        }
      }
      return false
    },
  },
})

// Watch for external content changes (e.g., auto-appended roll results, tab switches)
watch(() => props.content, (newContent) => {
  if (!editor.value) return
  // Skip if Tiptap already shows this content (avoids cursor reset on feedback loops)
  const currentMarkdown = editor.value.getMarkdown()
  if (newContent === currentMarkdown) return

  isUpdatingFromProp.value = true
  editor.value.commands.setContent(newContent ?? '', { contentType: 'markdown' })
  nextTick(() => {
    isUpdatingFromProp.value = false
  })
})

// Watch disabled prop
watch(() => props.disabled, (disabled) => {
  editor.value?.setEditable(!disabled)
})

// Watch enhanced prop — update storage and re-set content to trigger node view re-render
watch(() => props.enhanced, () => {
  if (!editor.value) return
  const storage = editor.value.storage as any
  if (storage.rollTable) storage.rollTable.enhanced = props.enhanced
  if (storage.noteBlock) storage.noteBlock.enhanced = props.enhanced
  const currentMarkdown = editor.value.getMarkdown()
  isUpdatingFromProp.value = true
  editor.value.commands.setContent(currentMarkdown, { contentType: 'markdown' })
  nextTick(() => {
    isUpdatingFromProp.value = false
  })
})

/** Focus the editor and place the cursor at the end of the document. */
function focusEnd() {
  const ed = editor.value
  if (!ed) return
  const end = ed.state.doc.content.size
  ed.commands.focus()
  ed.commands.setTextSelection(end)
}

defineExpose({ focusEnd })

onBeforeUnmount(() => {
  editor.value?.destroy()
})
</script>

<template>
  <div
    class="wysiwyg-editor markdown-body"
    :style="fontStyle"
  >
    <EditorContent v-if="editor" :editor="editor" />
  </div>
</template>
