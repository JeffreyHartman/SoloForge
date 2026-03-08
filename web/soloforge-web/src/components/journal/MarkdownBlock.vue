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
 * Converts [[Note Name]] and [[path|display]] wiki-link syntax to clickable anchor elements.
 * Supports alias form for disambiguating notes with identical basenames.
 */
function processWikiLinks(text: string): string {
  return text.replace(/\[\[([^\]]+)\]\]/g, (_match, inner: string) => {
    const trimmed = inner.trim()
    // Support alias form: [[path|display]]
    const pipeIdx = trimmed.indexOf('|')
    const pathPart = pipeIdx >= 0 ? trimmed.substring(0, pipeIdx).trim() : trimmed
    const displayPart = pipeIdx >= 0 ? trimmed.substring(pipeIdx + 1).trim() : trimmed
    const resolvedPath = pathPart.endsWith('.md') ? pathPart : `${pathPart}.md`
    const escapedDisplay = displayPart.replace(/"/g, '&quot;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
    const escapedPath = resolvedPath.replace(/"/g, '&quot;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
    return `<a class="wiki-link" href="#" role="link" tabindex="0" data-path="${escapedPath}">${escapedDisplay}</a>`
  })
}

const html = computed(() => {
  if (!props.content.trim()) return ''
  const processed = processWikiLinks(props.content)
  return DOMPurify.sanitize(md.render(processed), { ADD_ATTR: ['data-path'] })
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
