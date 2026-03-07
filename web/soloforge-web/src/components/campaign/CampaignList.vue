<script setup lang="ts">
import type { CampaignSummary } from '../../types'
import BaseCard from '../common/BaseCard.vue'
import BaseButton from '../common/BaseButton.vue'
import BaseInput from '../common/BaseInput.vue'
import { ref } from 'vue'

defineProps<{
  campaigns: CampaignSummary[]
  currentCampaignId: string | null
  loading: boolean
  loadingCreate: boolean
  loadingLoad: boolean
  loadingDelete: boolean
  apiOnline: boolean
}>()

const emit = defineEmits<{
  create: [name: string]
  load: [id: string]
  delete: [id: string]
}>()

const newCampaignName = ref('')

function formatDate(value: string | null | undefined): string {
  if (!value) return ''
  const d = new Date(value)
  if (Number.isNaN(d.getTime())) return value
  return d.toLocaleString()
}

function handleCreate() {
  const name = newCampaignName.value.trim()
  if (name) {
    emit('create', name)
    newCampaignName.value = ''
  }
}

function handleDelete(id: string, name: string) {
  const ok = window.confirm(`Delete campaign "${name}"? This cannot be undone.`)
  if (ok) {
    emit('delete', id)
  }
}
</script>

<template>
  <BaseCard title="Campaigns">
    <template #header>
      <div class="text-xs text-[var(--color-text-dimmed)]">{{ campaigns.length }} total</div>
    </template>

    <div class="flex gap-2">
      <BaseInput
        v-model="newCampaignName"
        placeholder="New campaign name"
        class="flex-1"
        @enter="handleCreate"
      />
      <BaseButton
        variant="warning"
        :disabled="loadingCreate || !newCampaignName.trim() || !apiOnline"
        :loading="loadingCreate"
        @click="handleCreate"
      >
        Create
      </BaseButton>
    </div>

    <div class="mt-4 max-h-[320px] overflow-auto rounded-2xl border border-[var(--color-border-card)] bg-[var(--color-bg-card-solid)]">
      <div v-if="loading" class="p-4 text-sm text-[var(--color-text-muted)]">Loading campaigns...</div>
      <div v-else-if="campaigns.length === 0" class="p-4 text-sm text-[var(--color-text-muted)]">No campaigns found.</div>
      <ul v-else class="divide-y divide-[var(--color-border-primary)]">
        <li
          v-for="c in campaigns"
          :key="c.id"
          class="flex items-center justify-between gap-3 p-3"
        >
          <div class="min-w-0">
            <div class="flex items-center gap-2">
              <div class="truncate text-sm font-semibold text-[var(--color-text-primary)]">
                {{ c.name }}
              </div>
              <span
                v-if="c.id === currentCampaignId"
                class="rounded-full bg-[var(--color-bg-success)]/20 px-2 py-0.5 text-[11px] font-semibold text-[var(--color-text-success)]"
              >
                current
              </span>
            </div>
            <div class="mt-0.5 text-xs text-[var(--color-text-dimmed)]">
              Last played: {{ formatDate(c.lastPlayed) }} · Entries: {{ c.historyCount }}
            </div>
          </div>
          <div class="flex items-center gap-2">
            <BaseButton
              variant="secondary"
              size="sm"
              :disabled="loadingLoad || c.id === currentCampaignId || !apiOnline"
              @click="$emit('load', c.id)"
            >
              Load
            </BaseButton>
            <BaseButton
              variant="danger"
              size="sm"
              :disabled="loadingDelete || campaigns.length <= 1 || !apiOnline"
              @click="handleDelete(c.id, c.name)"
            >
              Delete
            </BaseButton>
          </div>
        </li>
      </ul>
    </div>
  </BaseCard>
</template>
