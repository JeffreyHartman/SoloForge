import { useCampaign } from './useCampaign'
import { useHistory } from './useHistory'
import { useJournal } from './useJournal'
import { useNotes } from './useNotes'
import { useApi } from './useApi'

export function useToolActions() {
  const campaign = useCampaign()
  const historyState = useHistory()
  const journalState = useJournal()
  const notesState = useNotes()
  const { setError, clearError, apiOnline } = useApi()

  async function refreshAfterAction() {
    await campaign.refreshState()
    await historyState.refreshHistory()
    await journalState.refreshJournal(campaign.currentCampaignId.value)
    const logPath = notesState.sessionLogPath.value
    notesState.invalidateTabCache(logPath)
    if (notesState.activeNotePath.value === logPath) {
      await notesState.reloadActiveNote()
    }
  }

  async function runAction(fn: () => Promise<unknown>) {
    clearError()
    try {
      await fn()
      await refreshAfterAction()
    } catch (err) {
      setError(err)
    }
  }

  return {
    apiOnline,
    refreshAfterAction,
    runAction,
    setError,
    clearError,
  }
}
