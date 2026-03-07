<script setup lang="ts">
import JournalEditor from '../components/journal/JournalEditor.vue'

defineProps<{
  campaignId: string | null
  loading: boolean
  loadingSave: boolean
  apiOnline: boolean
}>()

const content = defineModel<string>('content')

defineEmits<{
  reload: []
  save: []
}>()
</script>

<template>
  <JournalEditor
    :campaign-id="campaignId"
    :loading="loading"
    :loading-save="loadingSave"
    :api-online="apiOnline"
    v-model:content="content"
    @reload="$emit('reload')"
    @save="$emit('save')"
  />
</template>
