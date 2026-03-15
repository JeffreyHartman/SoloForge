<script setup lang="ts">
import { ref, watch, onBeforeUnmount, nextTick } from 'vue'
import { Selection } from '@tiptap/pm/state'
import { useEditor, EditorContent } from '@tiptap/vue-3'
import StarterKit from '@tiptap/starter-kit'
import HardBreak from '@tiptap/extension-hard-break'
import { Table, TableRow, TableCell, TableHeader } from '@tiptap/extension-table'
import Placeholder from '@tiptap/extension-placeholder'
import { Markdown } from 'tiptap-markdown'
import { RollTableNode } from './tiptap/RollTableNode'
import { NoteBlockNode } from './tiptap/NoteBlockNode'
import { WikiLinkMark } from './tiptap/WikiLinkMark'
import { createWikiLinkSuggestion } from './tiptap/wikiLinkSuggestion'
import { preprocessForWysiwyg } from './tiptap/preprocessMarkdown'

const props = defineProps<{
  content: string | undefined
  fontStyle: Record<string, string | undefined>
  disabled: boolean
  placeholder: string
  allPaths?: string[]
}>()

const emit = defineEmits<{
  'update:content': [value: string]
  navigate: [path: string]
}>()

const isUpdatingFromProp = ref(false)

const editor = useEditor({
  content: preprocessForWysiwyg(props.content ?? ''),
  editable: !props.disabled,
  extensions: [
    StarterKit.configure({ hardBreak: false }),
    HardBreak.extend({
      addKeyboardShortcuts() {
        return {
          // Enter → hard break (single newline) only inside paragraphs.
          // In headings/other blocks, fall through to default (exits the block).
          Enter: () => {
            const { $from } = this.editor.state.selection
            if ($from.parent.type.name !== 'paragraph') return false
            return this.editor.commands.setHardBreak()
          },
        }
      },
      addStorage() {
        return {
          markdown: {
            serialize(state: any, node: any, parent: any, index: number) {
              // Only emit newline if there's real content after this break
              for (let i = index + 1; i < parent.childCount; i++) {
                if (parent.child(i).type !== node.type) {
                  state.write('\n')
                  return
                }
              }
            },
            parse: {},
          },
        }
      },
    }),
    Table.configure({ resizable: false }),
    TableRow,
    TableCell,
    TableHeader,
    Placeholder.configure({ placeholder: props.placeholder }),
    Markdown.configure({
      html: true,
      tightLists: true,
      breaks: true,
      transformPastedText: true,
      transformCopiedText: true,
    }),
    RollTableNode,
    NoteBlockNode,
    WikiLinkMark,
    createWikiLinkSuggestion({
      allPaths: () => props.allPaths ?? [],
    }),
  ],
  onUpdate({ editor: ed }) {
    if (isUpdatingFromProp.value) return
    const md = (ed.storage as any).markdown.getMarkdown() as string
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
    handleKeyDown(_view, event) {
      if (event.key === 'Enter') {
        const target = (event.target as HTMLElement).closest('.wiki-link') as HTMLElement | null
        if (target?.dataset.path) {
          event.preventDefault()
          emit('navigate', target.dataset.path)
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
  const currentMarkdown = (editor.value.storage as any).markdown.getMarkdown() as string
  if (newContent === currentMarkdown) return

  isUpdatingFromProp.value = true
  const preprocessed = preprocessForWysiwyg(newContent ?? '')
  editor.value.commands.setContent(preprocessed)
  nextTick(() => {
    isUpdatingFromProp.value = false
  })
})

// Watch disabled prop
watch(() => props.disabled, (disabled) => {
  editor.value?.setEditable(!disabled)
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
