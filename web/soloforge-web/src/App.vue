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

import AppHeader from './components/layout/AppHeader.vue'
import AppNav from './components/layout/AppNav.vue'
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
      session.refreshThemes(),
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

// Mythic actions
async function runFateCheck() {
  clearError()
  try {
    await mythic.runFateCheck()
    await refreshAfterAction()
  } catch (err) {
    setError(err)
  }
}

async function runSceneCheck() {
  clearError()
  try {
    await mythic.runSceneCheck()
    await refreshAfterAction()
  } catch (err) {
    setError(err)
  }
}

async function runRandomEvent() {
  clearError()
  try {
    await mythic.runRandomEvent()
    await refreshAfterAction()
  } catch (err) {
    setError(err)
  }
}

async function runMeaning() {
  clearError()
  try {
    await mythic.runMeaning()
    await refreshAfterAction()
  } catch (err) {
    setError(err)
  }
}

async function rollDice(expr?: string) {
  clearError()
  try {
    await mythic.rollDice(expr)
    await refreshAfterAction()
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

// Journal actions
async function reloadJournal() {
  clearError()
  try {
    await journalState.refreshJournal(campaign.currentCampaignId.value)
  } catch (err) {
    setError(err)
  }
}

// Helper
async function refreshAfterAction() {
  await campaign.refreshState()
  await historyState.refreshHistory()
  await journalState.refreshJournal(campaign.currentCampaignId.value)
  // Invalidate stale cache for the session log so the next open shows fresh content
  const logPath = notesState.sessionLogPath.value
  notesState.invalidateTabCache(logPath)
  // If the session log note is currently active, reload it immediately
  if (notesState.activeNotePath.value === logPath) {
    await notesState.reloadActiveNote()
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

    <div v-if="errorMessage" class="relative mx-auto max-w-6xl px-4 pt-4">
      <div class="rounded-2xl border border-[var(--color-border-danger)] bg-[var(--color-bg-danger)] px-4 py-3 text-sm text-[var(--color-text-danger)]">
        {{ errorMessage }}
      </div>
    </div>

    <main class="relative mx-auto max-w-6xl px-4 pb-12 pt-6">
      <DashboardView
        v-if="currentView === 'dashboard'"
        :campaigns="campaign.campaigns.value"
        :current-campaign="campaign.currentCampaign.value"
        :current-campaign-id="campaign.currentCampaignId.value"
        :chaos="campaign.session.value.chaos"
        :themes="session.themes.value"
        :loading-campaigns="campaign.loading.campaigns"
        :loading-create="campaign.loading.createCampaign"
        :loading-load="campaign.loading.loadCampaign"
        :loading-delete="campaign.loading.deleteCampaign"
        :loading-session="session.loading.updateSession"
        :api-online="apiOnline ?? false"
        v-model:chaos-draft="session.chaosDraft.value"
        v-model:engine-draft="session.engineDraft.value"
        v-model:theme-draft="session.themeDraft.value"
        @create-campaign="createCampaign"
        @load-campaign="loadCampaign"
        @delete-campaign="deleteCampaign"
        @update-session="updateSession"
      />

      <ToolsView
        v-else-if="currentView === 'tools'"
        :chaos="campaign.session.value.chaos"
        :fate-result="mythic.fateResult.value"
        :scene-result="mythic.sceneResult.value"
        :random-result="mythic.randomResult.value"
        :meaning-result="mythic.meaningResult.value"
        :meaning-meta="mythic.meaningMeta.value"
        :quick-set-result="mythic.quickSetResult.value"
        :dice-result="mythic.diceResult.value"
        :table-groups="tables.tableGroups.value"
        :quick-sets="tables.quickSets.value"
        :loading-fate="mythic.loading.fateCheck"
        :loading-scene="mythic.loading.sceneCheck"
        :loading-random="mythic.loading.randomEvent"
        :loading-meaning="mythic.loading.meaning"
        :loading-dice="mythic.loading.diceRoll"
        :loading-add-npc="adventure.loading.addCharacter"
        :api-online="apiOnline ?? false"
        v-model:fate-odds="mythic.fateOdds.value"
        v-model:fate-question="mythic.fateQuestion.value"
        v-model:scene-context="mythic.sceneContext.value"
        v-model:meaning-mode="mythic.meaningMode.value"
        v-model:meaning-context="mythic.meaningContext.value"
        v-model:meaning-table-id="mythic.meaningTableId.value"
        v-model:meaning-fusion-table1="mythic.meaningFusionTable1.value"
        v-model:meaning-fusion-table2="mythic.meaningFusionTable2.value"
        v-model:meaning-quick-set-id="mythic.meaningQuickSetId.value"
        v-model:dice-expression="mythic.diceExpression.value"
        @fate-check="runFateCheck"
        @scene-check="runSceneCheck"
        @random-event="runRandomEvent"
        @meaning="runMeaning"
        @dice-roll="rollDice"
        @add-npc="addCharacter"
      />

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
    </main>
  </div>
</template>
