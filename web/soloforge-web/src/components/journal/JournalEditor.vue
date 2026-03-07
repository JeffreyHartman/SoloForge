<script setup lang="ts">
import BaseCard from '../common/BaseCard.vue'
import BaseButton from '../common/BaseButton.vue'

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
  <BaseCard title="Journal">
    <template #header>
      <div class="flex items-center gap-2">
        <BaseButton
          variant="secondary"
          size="sm"
          :disabled="loading || !apiOnline || !campaignId"
          @click="$emit('reload')"
        >
          Reload
        </BaseButton>
        <BaseButton
          variant="success"
          size="sm"
          :disabled="loadingSave || !campaignId || !apiOnline"
          :loading="loadingSave"
          @click="$emit('save')"
        >
          Save
        </BaseButton>
      </div>
    </template>

    <textarea
      v-model="content"
      aria-label="Journal content"
      class="h-[calc(100vh-16rem)] min-h-[420px] w-full resize-none rounded-2xl border border-[var(--color-border-primary)] bg-[var(--color-bg-input)] p-4 font-mono text-[13px] leading-5 text-[var(--color-text-primary)] shadow-sm outline-none transition placeholder:text-[var(--color-text-dimmed)] focus:border-[var(--color-text-dimmed)] focus:shadow"
      :placeholder="campaignId ? 'Journal markdown...' : 'Load or create a campaign first.'"
      :disabled="!campaignId"
    />
    <div class="mt-2 text-xs text-[var(--color-text-dimmed)]">
      Saved in your local <code class="rounded bg-[var(--color-bg-muted)] px-1 py-0.5 font-mono text-[11px]">saves/</code> folder as markdown. This is plain text; rendering comes later.
    </div>
  </BaseCard>
</template>
