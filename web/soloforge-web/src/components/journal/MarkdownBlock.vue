<script setup lang="ts">
import { computed } from 'vue'
import MarkdownIt from 'markdown-it'
import DOMPurify from 'dompurify'

const props = defineProps<{ content: string }>()

const md = new MarkdownIt({
  html: true,
  linkify: true,
  typographer: true,
})

const html = computed(() => {
  if (!props.content.trim()) return ''
  return DOMPurify.sanitize(md.render(props.content))
})
</script>

<template>
  <div v-if="html" class="markdown-body" v-html="html" />
</template>
