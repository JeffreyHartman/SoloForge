<script setup lang="ts">
import { computed } from 'vue'
import { findPage } from '../../tools/registry'
import BaseCard from '../common/BaseCard.vue'

const props = defineProps<{
  pageId: string
}>()

const pageName = computed(() => findPage(props.pageId)?.name ?? props.pageId)
</script>

<template>
  <div class="max-w-lg">
    <BaseCard :title="pageName">
      <div class="py-12 text-center text-sm text-[var(--color-text-dimmed)]">
        Coming soon.
      </div>
    </BaseCard>
  </div>
</template>
