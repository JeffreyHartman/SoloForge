import { ref, reactive } from 'vue'
import type { ApiState, ThemeSummary } from '../types'
import { apiGet, apiSend } from './useApi'

const MIN_CHAOS = 1
const MAX_CHAOS = 9

const themes = ref<ThemeSummary[]>([])
const chaosDraft = ref<number>(5)
const engineDraft = ref<string>('Mythic 2e')
const themeDraft = ref<string>('Fantasy')

const loading = reactive({
  themes: false,
  updateSession: false,
})

export function useSession() {
  async function refreshThemes() {
    loading.themes = true
    try {
      themes.value = await apiGet<ThemeSummary[]>('/api/themes')
    } finally {
      loading.themes = false
    }
  }

  async function updateSession(): Promise<ApiState> {
    let chaos = Number(chaosDraft.value)
    if (!Number.isFinite(chaos)) {
      chaos = 5
    }
    chaos = Math.max(MIN_CHAOS, Math.min(MAX_CHAOS, chaos))
    const engine = engineDraft.value.trim()
    const theme = themeDraft.value.trim()
    loading.updateSession = true
    try {
      return await apiSend<ApiState>('/api/session', 'PUT', { chaos, engine, theme })
    } finally {
      loading.updateSession = false
    }
  }

  function syncFromState(session: { chaos: number; engine: string; theme: string }) {
    chaosDraft.value = session.chaos
    engineDraft.value = session.engine
    themeDraft.value = session.theme
  }

  return {
    themes,
    chaosDraft,
    engineDraft,
    themeDraft,
    loading,
    refreshThemes,
    updateSession,
    syncFromState,
  }
}
