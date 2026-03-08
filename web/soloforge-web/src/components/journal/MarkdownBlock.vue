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

// Allow data-path attribute through DOMPurify
DOMPurify.addHook('uponSanitizeAttribute', (_node, data) => {
  if (data.attrName === 'data-path') {
    data.forceKeepAttr = true
  }
})

// Convert [[Note Name]] to clickable wiki-links before markdown rendering
function processWikiLinks(text: string): string {
  return text.replace(/\[\[([^\]]+)\]\]/g, (_match, name: string) => {
    const trimmed = name.trim()
    const path = trimmed.endsWith('.md') ? trimmed : `${trimmed}.md`
    const escaped = trimmed.replace(/"/g, '&quot;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
    const escapedPath = path.replace(/"/g, '&quot;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
    return `<a class="wiki-link" data-path="${escapedPath}">${escaped}</a>`
  })
}

const html = computed(() => {
  if (!props.content.trim()) return ''
  const processed = processWikiLinks(props.content)
  return DOMPurify.sanitize(md.render(processed))
})

function handleClick(e: MouseEvent) {
  const target = (e.target as HTMLElement).closest('.wiki-link') as HTMLElement | null
  if (target?.dataset.path) {
    e.preventDefault()
    emit('navigate', target.dataset.path)
  }
}
</script>

<template>
  <div v-if="html" class="markdown-body" v-html="html" @click="handleClick" />
</template>
