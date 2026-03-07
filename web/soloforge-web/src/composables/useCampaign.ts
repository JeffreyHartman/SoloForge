import { ref, computed, reactive } from 'vue'
import type { ApiState, CampaignSummary } from '../types'
import { apiGet, apiSend } from './useApi'

const state = ref<ApiState | null>(null)
const campaigns = ref<CampaignSummary[]>([])

const loading = reactive({
  state: false,
  campaigns: false,
  createCampaign: false,
  loadCampaign: false,
  deleteCampaign: false,
})

export function useCampaign() {
  const currentCampaignName = computed(() => state.value?.currentCampaign?.name ?? 'No campaign loaded')
  const currentCampaignId = computed(() => state.value?.currentCampaign?.id ?? null)
  const currentCampaign = computed(() => state.value?.currentCampaign ?? null)
  const adventure = computed(() => state.value?.adventure ?? { characters: [], activeThreads: [], closedThreads: [] })
  const session = computed(() => state.value?.session ?? { chaos: 5, engine: 'Mythic 2e', theme: 'Fantasy' })

  async function refreshState() {
    loading.state = true
    try {
      state.value = await apiGet<ApiState>('/api/state')
    } finally {
      loading.state = false
    }
  }

  async function refreshCampaigns() {
    loading.campaigns = true
    try {
      campaigns.value = await apiGet<CampaignSummary[]>('/api/campaigns')
    } finally {
      loading.campaigns = false
    }
  }

  async function createCampaign(name: string): Promise<ApiState> {
    loading.createCampaign = true
    try {
      state.value = await apiSend<ApiState>('/api/campaigns', 'POST', { name })
      await refreshCampaigns()
      return state.value
    } finally {
      loading.createCampaign = false
    }
  }

  async function loadCampaign(id: string): Promise<ApiState> {
    loading.loadCampaign = true
    try {
      state.value = await apiSend<ApiState>(`/api/campaigns/${id}/load`, 'POST')
      await refreshCampaigns()
      return state.value
    } finally {
      loading.loadCampaign = false
    }
  }

  async function deleteCampaign(id: string): Promise<void> {
    loading.deleteCampaign = true
    try {
      await apiSend<{ deleted: boolean }>(`/api/campaigns/${id}`, 'DELETE')
      await refreshState()
      await refreshCampaigns()
    } finally {
      loading.deleteCampaign = false
    }
  }

  function setState(newState: ApiState) {
    state.value = newState
  }

  return {
    state,
    campaigns,
    loading,
    currentCampaignName,
    currentCampaignId,
    currentCampaign,
    adventure,
    session,
    refreshState,
    refreshCampaigns,
    createCampaign,
    loadCampaign,
    deleteCampaign,
    setState,
  }
}
