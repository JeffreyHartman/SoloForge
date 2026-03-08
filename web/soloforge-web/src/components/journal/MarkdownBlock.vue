<script setup lang="ts">
import { computed } from 'vue'
import MarkdownIt from 'markdown-it'
import DOMPurify from 'dompurify'

const props = defineProps<{ content: string }>()

const emit = defineEmits<{
  navigate: [path: string]
}>()

const md = new MarkdownIt({
  html: true,
  linkify: true,
  typographer: true,
})

/**
 * Markdown-it inline rule that converts [[Note Name]] and [[path|display]] wiki-link syntax
 * into clickable anchor tokens. Runs during tokenization so code blocks/spans are not affected.
 */
function wikiLinkPlugin(mdi: MarkdownIt) {
  mdi.inline.ruler.push('wiki_link', (state) => {
    const src = state.src
    const pos = state.pos
    if (src.charCodeAt(pos) !== 0x5B || src.charCodeAt(pos + 1) !== 0x5B) return false

    const end = src.indexOf(']]', pos + 2)
    if (end === -1) return false

    const inner = src.substring(pos + 2, end).trim()
    if (!inner || inner.includes('\n')) return false

    if (!state.env) state.env = {}
    // Only advance and create tokens when not in silent (validation) mode
    if (!state.md) return true
    if (state.pos >= state.posMax) return false

    const pipeIdx = inner.indexOf('|')
    const pathPart = pipeIdx >= 0 ? inner.substring(0, pipeIdx).trim() : inner
    const displayPart = pipeIdx >= 0 ? inner.substring(pipeIdx + 1).trim() : inner
    const resolvedPath = pathPart.endsWith('.md') ? pathPart : `${pathPart}.md`

    const tokenOpen = state.push('html_inline', '', 0)
    const escapedPath = resolvedPath.replace(/"/g, '&quot;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
    tokenOpen.content = `<a class="wiki-link" href="#" role="link" tabindex="0" data-path="${escapedPath}">`

    const tokenText = state.push('html_inline', '', 0)
    tokenText.content = displayPart.replace(/</g, '&lt;').replace(/>/g, '&gt;')

    const tokenClose = state.push('html_inline', '', 0)
    tokenClose.content = '</a>'

    state.pos = end + 2
    return true
  })
}

md.use(wikiLinkPlugin)

const html = computed(() => {
  if (!props.content.trim()) return ''
  return DOMPurify.sanitize(md.render(props.content), { ADD_ATTR: ['data-path'] })
})

/** Handles click and keyboard activation on wiki-link anchors, emitting navigation events. */
function handleClick(e: MouseEvent | KeyboardEvent) {
  const target = (e.target as HTMLElement).closest('.wiki-link') as HTMLElement | null
  if (target?.dataset.path) {
    e.preventDefault()
    emit('navigate', target.dataset.path)
  }
}
</script>

<template>
  <div v-if="html" class="markdown-body" v-html="html" @click="handleClick" @keydown.enter="handleClick" />
</template>
