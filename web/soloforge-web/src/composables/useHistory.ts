import { ref, reactive } from 'vue'
import type { HistoryEntry } from '../types'
import { apiGet } from './useApi'

const history = ref<HistoryEntry[]>([])

const loading = reactive({
  history: false,
})

export function useHistory() {
  async function refreshHistory() {
    loading.history = true
    try {
      const entries = await apiGet<HistoryEntry[]>('/api/history')
      history.value = entries
        .slice()
        .sort((a, b) => new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime())
    } finally {
      loading.history = false
    }
  }

  return {
    history,
    loading,
    refreshHistory,
  }
}
