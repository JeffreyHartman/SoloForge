<script setup lang="ts">
import type { CampaignSummary, CampaignInfo, ThemeSummary } from '../types'
import SessionSettings from '../components/campaign/SessionSettings.vue'
import CampaignList from '../components/campaign/CampaignList.vue'

defineProps<{
  campaigns: CampaignSummary[]
  currentCampaign: CampaignInfo | null
  currentCampaignId: string | null
  chaos: number
  themes: ThemeSummary[]
  loadingCampaigns: boolean
  loadingCreate: boolean
  loadingLoad: boolean
  loadingDelete: boolean
  loadingSession: boolean
  apiOnline: boolean
}>()

const chaosDraft = defineModel<number>('chaosDraft', { required: true })
const engineDraft = defineModel<string>('engineDraft', { required: true })
const themeDraft = defineModel<string>('themeDraft', { required: true })

const emit = defineEmits<{
  createCampaign: [name: string]
  loadCampaign: [id: string]
  deleteCampaign: [id: string]
  updateSession: []
}>()
</script>

<template>
  <div class="grid grid-cols-1 gap-6 lg:grid-cols-2">
    <SessionSettings
      :current-campaign="currentCampaign"
      :chaos="chaos"
      :themes="themes"
      :loading="loadingSession"
      :api-online="apiOnline"
      v-model:chaos-draft="chaosDraft"
      v-model:engine-draft="engineDraft"
      v-model:theme-draft="themeDraft"
      @apply="emit('updateSession')"
    />

    <CampaignList
      :campaigns="campaigns"
      :current-campaign-id="currentCampaignId"
      :loading="loadingCampaigns"
      :loading-create="loadingCreate"
      :loading-load="loadingLoad"
      :loading-delete="loadingDelete"
      :api-online="apiOnline"
      @create="(name) => emit('createCampaign', name)"
      @load="(id) => emit('loadCampaign', id)"
      @delete="(id) => emit('deleteCampaign', id)"
    />
  </div>
</template>
