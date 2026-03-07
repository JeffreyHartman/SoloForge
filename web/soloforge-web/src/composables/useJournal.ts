import { ref, reactive } from 'vue'
import { apiGet, apiSend } from './useApi'

const journal = ref<string>('')
const currentCampaignId = ref<string | null>(null)

const loading = reactive({
  journal: false,
  saveJournal: false,
})

export function useJournal() {
  async function refreshJournal(campaignId: string | null) {
    if (!campaignId) {
      journal.value = ''
      currentCampaignId.value = null
      return
    }

    currentCampaignId.value = campaignId
    loading.journal = true
    try {
      const result = await apiGet<{ campaignId: string; content: string }>(`/api/journal?campaignId=${encodeURIComponent(campaignId)}`)
      journal.value = result.content ?? ''
    } finally {
      loading.journal = false
    }
  }

  async function saveJournal(): Promise<void> {
    if (!currentCampaignId.value) {
      throw new Error('No campaign loaded')
    }
    loading.saveJournal = true
    try {
      await apiSend<{ saved: boolean }>('/api/journal', 'PUT', {
        campaignId: currentCampaignId.value,
        content: journal.value,
      })
    } finally {
      loading.saveJournal = false
    }
  }

  return {
    journal,
    loading,
    refreshJournal,
    saveJournal,
  }
}
