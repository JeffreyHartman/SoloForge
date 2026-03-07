import { ref, reactive, watch } from 'vue'
import { apiGet, apiSend } from './useApi'

const journal = ref<string>('')
const currentCampaignId = ref<string | null>(null)
const saveStatus = ref<'saved' | 'saving' | 'unsaved'>('saved')

const loading = reactive({
  journal: false,
  saveJournal: false,
})

// Auto-save internals
let lastSavedContent = ''
let saving = false
let pendingSave = false
let debounceTimer: ReturnType<typeof setTimeout> | null = null
const DEBOUNCE_MS = 3000
let initialized = false

function scheduleSave() {
  if (debounceTimer) clearTimeout(debounceTimer)
  debounceTimer = setTimeout(() => void executeSave(), DEBOUNCE_MS)
}

async function executeSave() {
  if (!currentCampaignId.value) return

  // Loop until content is stable (replaces recursion)
  while (true) {
    if (journal.value === lastSavedContent) {
      saveStatus.value = 'saved'
      return
    }
    if (saving) {
      pendingSave = true
      return
    }

    saving = true
    loading.saveJournal = true
    saveStatus.value = 'saving'
    const contentAtStart = journal.value
    const campaignId = currentCampaignId.value

    try {
      await apiSend<{ saved: boolean }>('/api/journal', 'PUT', {
        campaignId,
        content: contentAtStart,
      })
      lastSavedContent = contentAtStart
    } catch {
      saveStatus.value = 'unsaved'
      saving = false
      loading.saveJournal = false
      return
    }

    saving = false
    loading.saveJournal = false

    if (!pendingSave && journal.value === contentAtStart) {
      saveStatus.value = 'saved'
      return
    }
    pendingSave = false
  }
}

function flushSave() {
  if (debounceTimer) {
    clearTimeout(debounceTimer)
    debounceTimer = null
  }
  if (journal.value !== lastSavedContent && currentCampaignId.value) {
    void executeSave()
  }
}

function onBeforeUnload() {
  if (journal.value === lastSavedContent || !currentCampaignId.value) return
  fetch('/api/journal', {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ campaignId: currentCampaignId.value, content: journal.value }),
    keepalive: true,
  })
}

export function useJournal() {
  if (!initialized) {
    initialized = true

    watch(journal, (newVal) => {
      if (newVal === lastSavedContent) {
        saveStatus.value = 'saved'
        return
      }
      saveStatus.value = 'unsaved'
      scheduleSave()
    })

    window.addEventListener('beforeunload', onBeforeUnload)
  }

  async function refreshJournal(campaignId: string | null) {
    flushSave()

    if (!campaignId) {
      journal.value = ''
      currentCampaignId.value = null
      lastSavedContent = ''
      saveStatus.value = 'saved'
      return
    }

    currentCampaignId.value = campaignId
    loading.journal = true
    try {
      const result = await apiGet<{ campaignId: string; content: string }>(`/api/journal?campaignId=${encodeURIComponent(campaignId)}`)
      journal.value = result.content ?? ''
      lastSavedContent = journal.value
      saveStatus.value = 'saved'
    } finally {
      loading.journal = false
    }
  }

  return {
    journal,
    loading,
    saveStatus,
    refreshJournal,
    flushSave,
  }
}
