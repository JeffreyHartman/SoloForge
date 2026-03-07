<script setup lang="ts">
import JournalEditor from '../components/journal/JournalEditor.vue'

defineProps<{
  campaignId: string | null
  loading: boolean
  apiOnline: boolean
}>()

const content = defineModel<string>('content')

defineEmits<{
  reload: []
}>()
</script>

<template>
  <JournalEditor
    :campaign-id="campaignId"
    :loading="loading"
    :api-online="apiOnline"
    v-model:content="content"
    @reload="$emit('reload')"
  />
</template>
