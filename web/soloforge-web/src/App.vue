<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import type { ViewName } from './types'
import {
  useApi,
  useTheme,
  useCampaign,
  useSession,
  useJournal,
  useHistory,
  useAdventure,
  useTables,
  useMythic,
} from './composables'
import { useNotes } from './composables/useNotes'
import { useResultBanner } from './composables/useResultBanner'

import AppHeader from './components/layout/AppHeader.vue'
import AppNav from './components/layout/AppNav.vue'
import ToastContainer from './components/common/ToastContainer.vue'
import ResultBanner from './components/common/ResultBanner.vue'
import DashboardView from './views/DashboardView.vue'
import ToolsView from './views/ToolsView.vue'
import AdventureView from './views/AdventureView.vue'
import JournalView from './views/JournalView.vue'
import HistoryView from './views/HistoryView.vue'

// Composables
const { apiOnline, errorMessage, refreshHealth, setError, clearError } = useApi()
const { initTheme } = useTheme()
const campaign = useCampaign()
const session = useSession()
const journalState = useJournal()
const notesState = useNotes()
const historyState = useHistory()
const adventure = useAdventure()
const tables = useTables()
const mythic = useMythic()
const { clearLastEvent } = useResultBanner()

// View navigation
const currentView = ref<ViewName>('dashboard')

// Computed
const isBusy = computed(() =>
  campaign.loading.state ||
  campaign.loading.campaigns ||
  session.loading.updateSession ||
  historyState.loading.history
)

// Initialization
async function refreshAll() {
  clearError()
  await refreshHealth()
  if (apiOnline.value === false) return

  try {
    await campaign.refreshState()
    session.syncFromState(campaign.session.value)
    await Promise.all([
      campaign.refreshCampaigns(),
      tables.refreshTables(),
      tables.refreshQuickSets(),
      historyState.refreshHistory(),
      journalState.refreshJournal(campaign.currentCampaignId.value),
      notesState.refreshTree(campaign.currentCampaignId.value),
    ])
    mythic.initMeaningDefaults(
      tables.getFirstElementTable(),
      tables.getDefaultFusionTables(),
      tables.getFirstQuickSetId()
    )
  } catch (err) {
    setError(err)
  }
}

// Campaign actions
async function createCampaign(name: string) {
  clearError()
  try {
    notesState.flushSave()
    const state = await campaign.createCampaign(name)
    session.syncFromState(state.session)
    notesState.resetState()
    await Promise.all([
      journalState.refreshJournal(campaign.currentCampaignId.value),
      notesState.refreshTree(campaign.currentCampaignId.value),
    ])
  } catch (err) {
    setError(err)
  }
}

async function loadCampaign(id: string) {
  clearError()
  try {
    notesState.flushSave()
    const state = await campaign.loadCampaign(id)
    session.syncFromState(state.session)
    notesState.resetState()
    await historyState.refreshHistory()
    await Promise.all([
      journalState.refreshJournal(campaign.currentCampaignId.value),
      notesState.refreshTree(campaign.currentCampaignId.value),
    ])
    mythic.clearResults()
    clearLastEvent()
  } catch (err) {
    setError(err)
  }
}

async function deleteCampaign(id: string) {
  if (campaign.campaigns.value.length <= 1) return
  clearError()
  try {
    notesState.flushSave()
    await campaign.deleteCampaign(id)
    notesState.resetState()
    await historyState.refreshHistory()
    await Promise.all([
      journalState.refreshJournal(campaign.currentCampaignId.value),
      notesState.refreshTree(campaign.currentCampaignId.value),
    ])
  } catch (err) {
    setError(err)
  }
}

async function updateSession() {
  clearError()
  try {
    const state = await session.updateSession()
    campaign.setState(state)
    mythic.fateResult.value = null
    await campaign.refreshCampaigns()
  } catch (err) {
    setError(err)
  }
}

// Adventure actions
async function addCharacter(name: string, description: string) {
  clearError()
  try {
    await adventure.addCharacter(name, description || undefined)
    await campaign.refreshState()
    await campaign.refreshCampaigns()
  } catch (err) {
    setError(err)
  }
}

async function removeCharacter(name: string) {
  clearError()
  try {
    await adventure.removeCharacter(name)
    await campaign.refreshState()
    await campaign.refreshCampaigns()
  } catch (err) {
    setError(err)
  }
}

async function addThread(name: string, description: string) {
  clearError()
  try {
    await adventure.addThread(name, description || undefined)
    await campaign.refreshState()
    await campaign.refreshCampaigns()
  } catch (err) {
    setError(err)
  }
}

async function closeThread(name: string) {
  clearError()
  try {
    await adventure.closeThread(name)
    await campaign.refreshState()
    await campaign.refreshCampaigns()
  } catch (err) {
    setError(err)
  }
}

async function reopenThread(name: string) {
  clearError()
  try {
    await adventure.reopenThread(name)
    await campaign.refreshState()
    await campaign.refreshCampaigns()
  } catch (err) {
    setError(err)
  }
}

onMounted(() => {
  initTheme()
  void refreshAll()
})
</script>

<template>
  <div class="min-h-full bg-[var(--color-bg-primary)] text-[var(--color-text-primary)]">
    <div class="pointer-events-none fixed inset-0 opacity-[0.35] [background:var(--gradient-bg)]" />

    <AppHeader
      :campaign-name="campaign.currentCampaignName.value"
      :chaos="campaign.session.value.chaos"
      :api-online="apiOnline"
      :is-busy="isBusy"
      @refresh="refreshAll"
    />

    <AppNav
      :current-view="currentView"
      @navigate="currentView = $event"
    />

    <div v-if="errorMessage" class="relative mx-auto max-w-[2200px] px-4 pt-4">
      <div class="rounded-2xl border border-[var(--color-border-danger)] bg-[var(--color-bg-danger)] px-4 py-3 text-sm text-[var(--color-text-danger)]">
        {{ errorMessage }}
      </div>
    </div>

    <main class="relative mx-auto max-w-[2200px] px-4 pb-12 pt-6">
      <KeepAlive>
        <DashboardView
          v-if="currentView === 'dashboard'"
          :campaigns="campaign.campaigns.value"
          :current-campaign="campaign.currentCampaign.value"
          :current-campaign-id="campaign.currentCampaignId.value"
          :chaos="campaign.session.value.chaos"
          :loading-campaigns="campaign.loading.campaigns"
          :loading-create="campaign.loading.createCampaign"
          :loading-load="campaign.loading.loadCampaign"
          :loading-delete="campaign.loading.deleteCampaign"
          :loading-session="session.loading.updateSession"
          :api-online="apiOnline ?? false"
          v-model:chaos-draft="session.chaosDraft.value"
          v-model:engine-draft="session.engineDraft.value"
          @create-campaign="createCampaign"
          @load-campaign="loadCampaign"
          @delete-campaign="deleteCampaign"
          @update-session="updateSession"
        />

        <ToolsView v-else-if="currentView === 'tools'" />

        <AdventureView
          v-else-if="currentView === 'adventure'"
          :characters="campaign.adventure.value.characters"
          :active-threads="campaign.adventure.value.activeThreads"
          :closed-threads="campaign.adventure.value.closedThreads"
          :loading-add-character="adventure.loading.addCharacter"
          :loading-remove-character="adventure.loading.removeCharacter"
          :loading-add-thread="adventure.loading.addThread"
          :loading-close-thread="adventure.loading.closeThread"
          :loading-reopen-thread="adventure.loading.reopenThread"
          :api-online="apiOnline ?? false"
          @add-character="addCharacter"
          @remove-character="removeCharacter"
          @add-thread="addThread"
          @close-thread="closeThread"
          @reopen-thread="reopenThread"
        />

        <JournalView
          v-else-if="currentView === 'journal'"
          :campaign-id="campaign.currentCampaignId.value"
          :api-online="apiOnline ?? false"
        />

        <HistoryView
          v-else-if="currentView === 'history'"
          :entries="historyState.history.value"
          :loading="historyState.loading.history"
          :api-online="apiOnline ?? false"
          @refresh="historyState.refreshHistory"
        />
      </KeepAlive>
    </main>

    <ResultBanner />
    <ToastContainer />
  </div>
</template>
