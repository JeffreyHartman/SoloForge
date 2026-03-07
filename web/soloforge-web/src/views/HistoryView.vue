<script setup lang="ts">
import type { HistoryEntry } from '../types'
import HistoryList from '../components/history/HistoryList.vue'

defineProps<{
  entries: HistoryEntry[]
  loading: boolean
  apiOnline: boolean
}>()

defineEmits<{
  refresh: []
}>()
</script>

<template>
  <HistoryList
    :entries="entries"
    :loading="loading"
    :api-online="apiOnline"
    @refresh="$emit('refresh')"
  />
</template>
