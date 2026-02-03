<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'

type ApiState = {
  session: {
    chaos: number
    engine: string
    theme: string
    lastQuickRoll?: string | null
  }
  currentCampaign?: {
    id: string
    name: string
    createdAt: string
    lastPlayed: string
    historyCount: number
  } | null
  adventure: {
    characters: { name: string; description?: string | null; createdAt: string }[]
    activeThreads: { name: string; description?: string | null; createdAt: string; closedAt?: string | null }[]
    closedThreads: { name: string; description?: string | null; createdAt: string; closedAt?: string | null }[]
  }
  historyCount: number
}

type CampaignSummary = {
  id: string
  name: string
  createdAt: string
  lastPlayed: string
  chaos: number
  engine: string
  theme: string
  characterCount: number
  activeThreadCount: number
  closedThreadCount: number
  historyCount: number
}

type FateCheckResponse = {
  chaos: number
  odds: string
  fate: { roll: number; result: string; randomEventTriggered: boolean }
  randomEvent?:
    | {
        eventFocus: string
        eventAction: string
        selectedCharacter?: string | null
        selectedThread?: string | null
        isNewNpc: boolean
        listWasEmpty: boolean
      }
    | null
}

type SceneCheckResponse = {
  chaos: number
  scene: {
    roll: number
    result: string
    sceneAdjustment?: string | null
    randomEvent?: RandomEventResult | null
  }
}

type RandomEventResult = {
  eventFocus: string
  eventAction: string
  selectedCharacter?: string | null
  selectedThread?: string | null
  isNewNpc: boolean
  listWasEmpty: boolean
}

type TableInfo = {
  id: string
  displayName: string
  isElement: boolean
  category: string
}

type ThemeSummary = {
  name: string
  description: string
}

type MeaningResult = {
  tableName: string
  word1: string
  word2: string
  isFusion: boolean
  combined: string
}

type MeaningTableResponse = {
  table: { id: string; displayName: string }
  meaning: MeaningResult
}

type MeaningFusionResponse = {
  table1: { id: string; displayName: string }
  table2: { id: string; displayName: string }
  meaning: MeaningResult
}

type QuickSet = {
  id: string
  name: string
  description: string
  steps: { label: string; table: string; count: number }[]
}

type QuickSetResult = {
  quickSet: QuickSet
  results: { label: string; words: string[]; combined: string; tableId: string }[]
}

type HistoryEntry = {
  id: string
  timestamp: string
  type: string
  context?: string | null
  result: string
  details?: string | null
}

type DiceRollResponse = {
  roll: {
    summary: string
    total: number
    diceTotal: number
    modifier: number
    terms: { count: number; faces: number; sign: number; rolls: number[]; total: number }[]
  }
  breakdown: string
}

const loading = reactive({
  state: false,
  campaigns: false,
  journal: false,
  tables: false,
  quickSets: false,
  themes: false,
  history: false,
  createCampaign: false,
  loadCampaign: false,
  deleteCampaign: false,
  fateCheck: false,
  sceneCheck: false,
  randomEvent: false,
  meaning: false,
  quickSetGenerate: false,
  diceRoll: false,
  addCharacter: false,
  removeCharacter: false,
  addThread: false,
  closeThread: false,
  reopenThread: false,
  saveJournal: false,
  updateSession: false,
})

const errorMessage = ref<string | null>(null)
const apiOnline = ref<boolean | null>(null)

const state = ref<ApiState | null>(null)
const campaigns = ref<CampaignSummary[]>([])
const journal = ref<string>('')

const tables = ref<TableInfo[]>([])
const quickSets = ref<QuickSet[]>([])
const themes = ref<ThemeSummary[]>([])
const history = ref<HistoryEntry[]>([])

const newCampaignName = ref('')

const chaosDraft = ref<number>(5)
const engineDraft = ref<string>('Mythic 2e')
const themeDraft = ref<string>('Fantasy')

const oddsOptions = [
  { value: 'Impossible', label: 'Impossible' },
  { value: 'NearlyImpossible', label: 'Nearly Impossible' },
  { value: 'VeryUnlikely', label: 'Very Unlikely' },
  { value: 'Unlikely', label: 'Unlikely' },
  { value: 'FiftyFifty', label: '50/50' },
  { value: 'Likely', label: 'Likely' },
  { value: 'VeryLikely', label: 'Very Likely' },
  { value: 'NearlyCertain', label: 'Nearly Certain' },
  { value: 'Certain', label: 'Certain' },
]

const fateOdds = ref<string>('FiftyFifty')
const fateQuestion = ref<string>('')
const fateResult = ref<FateCheckResponse | null>(null)

const sceneContext = ref<string>('')
const sceneResult = ref<SceneCheckResponse | null>(null)

const randomResult = ref<RandomEventResult | null>(null)
const newNpcName = ref<string>('')
const newNpcDescription = ref<string>('')

const meaningMode = ref<'action' | 'description' | 'table' | 'fusion' | 'quickSet'>('action')
const meaningContext = ref<string>('')
const meaningTableId = ref<string>('')
const meaningFusionTable1 = ref<string>('')
const meaningFusionTable2 = ref<string>('')
const meaningQuickSetId = ref<string>('')
const meaningResult = ref<MeaningResult | null>(null)
const meaningMeta = ref<string | null>(null)
const quickSetResult = ref<QuickSetResult | null>(null)

const diceExpression = ref<string>('')
const diceResult = ref<DiceRollResponse | null>(null)

const characterName = ref<string>('')
const characterDescription = ref<string>('')
const threadName = ref<string>('')
const threadDescription = ref<string>('')

const currentCampaignName = computed(() => state.value?.currentCampaign?.name ?? 'No campaign loaded')
const currentCampaignId = computed(() => state.value?.currentCampaign?.id ?? null)
const isBusy = computed(() => Object.values(loading).some(v => Boolean(v)))

const tableGroups = computed(() => {
  const byCategory = new Map<string, TableInfo[]>()
  for (const t of tables.value) {
    const cat = t.category || (t.isElement ? 'Elements' : 'Core')
    const arr = byCategory.get(cat) ?? []
    arr.push(t)
    byCategory.set(cat, arr)
  }

  return Array.from(byCategory.entries())
    .map(([label, items]) => ({
      label,
      items: items.slice().sort((a, b) => a.displayName.localeCompare(b.displayName)),
    }))
    .sort((a, b) => a.label.localeCompare(b.label))
})

function formatDate(value: string | null | undefined): string {
  if (!value) return ''
  const d = new Date(value)
  if (Number.isNaN(d.getTime())) return value
  return d.toLocaleString()
}

async function apiGet<T>(path: string): Promise<T> {
  const resp = await fetch(path, { headers: { Accept: 'application/json' } })
  if (!resp.ok) {
    const text = await resp.text().catch(() => '')
    throw new Error(text || `${resp.status} ${resp.statusText}`)
  }
  return (await resp.json()) as T
}

async function apiSend<T>(path: string, method: string, body?: unknown): Promise<T> {
  const resp = await fetch(path, {
    method,
    headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body),
  })
  if (!resp.ok) {
    const text = await resp.text().catch(() => '')
    throw new Error(text || `${resp.status} ${resp.statusText}`)
  }
  return (await resp.json()) as T
}

async function refreshHealth() {
  try {
    await apiGet('/api/health')
    apiOnline.value = true
  } catch {
    apiOnline.value = false
  }
}

async function refreshState() {
  loading.state = true
  try {
    state.value = await apiGet<ApiState>('/api/state')
    chaosDraft.value = state.value.session.chaos
    engineDraft.value = state.value.session.engine
    themeDraft.value = state.value.session.theme
  } finally {
    loading.state = false
  }
}

async function refreshTables() {
  loading.tables = true
  try {
    tables.value = await apiGet<TableInfo[]>('/api/tables')

    // Initialize meaning selections the first time we get tables.
    if (!meaningTableId.value) {
      const firstElement = tables.value.find(t => t.isElement) ?? tables.value[0]
      if (firstElement) {
        meaningTableId.value = firstElement.id
      }
    }

    if (!meaningFusionTable1.value || !meaningFusionTable2.value) {
      const first = tables.value[0]
      const second = tables.value[1] ?? tables.value[0]
      if (first) meaningFusionTable1.value = first.id
      if (second) meaningFusionTable2.value = second.id
    }
  } finally {
    loading.tables = false
  }
}

async function refreshQuickSets() {
  loading.quickSets = true
  try {
    quickSets.value = await apiGet<QuickSet[]>('/api/quick-sets')
    if (!meaningQuickSetId.value && quickSets.value.length > 0) {
      const first = quickSets.value[0]
      if (first) {
        meaningQuickSetId.value = first.id
      }
    }
  } finally {
    loading.quickSets = false
  }
}

async function refreshThemes() {
  loading.themes = true
  try {
    themes.value = await apiGet<ThemeSummary[]>('/api/themes')
  } finally {
    loading.themes = false
  }
}

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

async function refreshCampaigns() {
  loading.campaigns = true
  try {
    campaigns.value = await apiGet<CampaignSummary[]>('/api/campaigns')
  } finally {
    loading.campaigns = false
  }
}

async function refreshJournal() {
  if (!currentCampaignId.value) {
    journal.value = ''
    return
  }

  loading.journal = true
  try {
    const result = await apiGet<{ campaignId: string; content: string }>('/api/journal')
    journal.value = result.content ?? ''
  } finally {
    loading.journal = false
  }
}

async function refreshAll() {
  errorMessage.value = null
  await refreshHealth()
  if (apiOnline.value === false) return

  try {
    await refreshState()
    await refreshCampaigns()
    await refreshTables()
    await refreshQuickSets()
    await refreshThemes()
    await refreshHistory()
    await refreshJournal()
  } catch (err) {
    errorMessage.value = err instanceof Error ? err.message : String(err)
  }
}

async function createCampaign() {
  const name = newCampaignName.value.trim()
  if (!name) return

  loading.createCampaign = true
  errorMessage.value = null
  try {
    state.value = await apiSend<ApiState>('/api/campaigns', 'POST', { name })
    newCampaignName.value = ''
    chaosDraft.value = state.value.session.chaos
    await refreshCampaigns()
    await refreshJournal()
  } catch (err) {
    errorMessage.value = err instanceof Error ? err.message : String(err)
  } finally {
    loading.createCampaign = false
  }
}

async function loadCampaign(id: string) {
  loading.loadCampaign = true
  errorMessage.value = null
  try {
    state.value = await apiSend<ApiState>(`/api/campaigns/${id}/load`, 'POST')
    chaosDraft.value = state.value.session.chaos
    await refreshCampaigns()
    await refreshHistory()
    await refreshJournal()
    fateResult.value = null
    sceneResult.value = null
    randomResult.value = null
    meaningResult.value = null
    meaningMeta.value = null
    quickSetResult.value = null
    diceResult.value = null
  } catch (err) {
    errorMessage.value = err instanceof Error ? err.message : String(err)
  } finally {
    loading.loadCampaign = false
  }
}

async function deleteCampaign(id: string) {
  if (campaigns.value.length <= 1) {
    return
  }

  const target = campaigns.value.find(c => c.id === id)
  const name = target?.name ?? id
  const ok = window.confirm(`Delete campaign "${name}"? This cannot be undone.`)
  if (!ok) return

  loading.deleteCampaign = true
  errorMessage.value = null
  try {
    await apiSend<{ deleted: boolean }>(`/api/campaigns/${id}`, 'DELETE')
    await refreshState()
    await refreshCampaigns()
    await refreshHistory()
    await refreshJournal()
  } catch (err) {
    errorMessage.value = err instanceof Error ? err.message : String(err)
  } finally {
    loading.deleteCampaign = false
  }
}

async function updateSession() {
  const chaos = Number(chaosDraft.value)
  const engine = engineDraft.value.trim()
  const theme = themeDraft.value.trim()
  loading.updateSession = true
  errorMessage.value = null
  try {
    state.value = await apiSend<ApiState>('/api/session', 'PUT', { chaos, engine, theme })
    fateResult.value = null
    await refreshCampaigns()
  } catch (err) {
    errorMessage.value = err instanceof Error ? err.message : String(err)
  } finally {
    loading.updateSession = false
  }
}

async function runFateCheck() {
  loading.fateCheck = true
  errorMessage.value = null
  try {
    fateResult.value = await apiSend<FateCheckResponse>('/api/fate-check', 'POST', {
      odds: fateOdds.value,
      question: fateQuestion.value.trim() || null,
    })
    await refreshState()
    await refreshHistory()
    await refreshJournal()
  } catch (err) {
    errorMessage.value = err instanceof Error ? err.message : String(err)
  } finally {
    loading.fateCheck = false
  }
}

async function runSceneCheck() {
  loading.sceneCheck = true
  errorMessage.value = null
  try {
    sceneResult.value = await apiSend<SceneCheckResponse>('/api/scene-check', 'POST', {
      context: sceneContext.value.trim() || null,
    })
    await refreshState()
    await refreshHistory()
    await refreshJournal()
  } catch (err) {
    errorMessage.value = err instanceof Error ? err.message : String(err)
  } finally {
    loading.sceneCheck = false
  }
}

async function runRandomEvent() {
  loading.randomEvent = true
  errorMessage.value = null
  try {
    randomResult.value = await apiSend<RandomEventResult>('/api/random-event', 'POST')
    await refreshState()
    await refreshHistory()
    await refreshJournal()
  } catch (err) {
    errorMessage.value = err instanceof Error ? err.message : String(err)
  } finally {
    loading.randomEvent = false
  }
}

async function addNpcFromRandomEvent() {
  const name = newNpcName.value.trim()
  if (!name) return

  loading.addCharacter = true
  errorMessage.value = null
  try {
    await apiSend('/api/adventure/characters', 'POST', {
      name,
      description: newNpcDescription.value.trim() || null,
    })
    newNpcName.value = ''
    newNpcDescription.value = ''
    await refreshState()
    await refreshCampaigns()
  } catch (err) {
    errorMessage.value = err instanceof Error ? err.message : String(err)
  } finally {
    loading.addCharacter = false
  }
}

async function runMeaning() {
  loading.meaning = true
  errorMessage.value = null
  meaningMeta.value = null
  try {
    const context = meaningContext.value.trim() || null

    if (meaningMode.value === 'action') {
      meaningResult.value = await apiSend<MeaningResult>('/api/meaning/action', 'POST', { context })
      quickSetResult.value = null
      return
    }

    if (meaningMode.value === 'description') {
      meaningResult.value = await apiSend<MeaningResult>('/api/meaning/description', 'POST', { context })
      quickSetResult.value = null
      return
    }

    if (meaningMode.value === 'table') {
      if (!meaningTableId.value) {
        throw new Error('Select a table first.')
      }

      const resp = await apiSend<MeaningTableResponse>('/api/meaning/table', 'POST', {
        tableId: meaningTableId.value,
        context,
      })
      meaningResult.value = resp.meaning
      meaningMeta.value = resp.table.displayName
      quickSetResult.value = null
      return
    }

    if (meaningMode.value === 'fusion') {
      if (!meaningFusionTable1.value || !meaningFusionTable2.value) {
        throw new Error('Select two tables first.')
      }

      const resp = await apiSend<MeaningFusionResponse>('/api/meaning/fusion', 'POST', {
        tableId1: meaningFusionTable1.value,
        tableId2: meaningFusionTable2.value,
        context,
      })
      meaningResult.value = resp.meaning
      meaningMeta.value = `${resp.table1.displayName} + ${resp.table2.displayName}`
      quickSetResult.value = null
      return
    }

    if (meaningMode.value === 'quickSet') {
      if (!meaningQuickSetId.value) {
        throw new Error('Select a quick set first.')
      }

      quickSetResult.value = await apiSend<QuickSetResult>('/api/quick-sets/generate', 'POST', {
        id: meaningQuickSetId.value,
        context,
      })
      meaningResult.value = null
      meaningMeta.value = null
      return
    }
  } catch (err) {
    errorMessage.value = err instanceof Error ? err.message : String(err)
  } finally {
    loading.meaning = false
    await refreshState()
    await refreshHistory()
    await refreshJournal()
  }
}

async function rollDice(expr?: string) {
  const expression = (expr ?? diceExpression.value).trim()
  if (!expression) return

  loading.diceRoll = true
  errorMessage.value = null
  try {
    diceResult.value = await apiSend<DiceRollResponse>('/api/dice-roll', 'POST', { expression })
    diceExpression.value = ''
    await refreshState()
    await refreshHistory()
    await refreshJournal()
  } catch (err) {
    errorMessage.value = err instanceof Error ? err.message : String(err)
  } finally {
    loading.diceRoll = false
  }
}

async function addCharacter() {
  const name = characterName.value.trim()
  if (!name) return

  loading.addCharacter = true
  errorMessage.value = null
  try {
    await apiSend('/api/adventure/characters', 'POST', {
      name,
      description: characterDescription.value.trim() || null,
    })

    characterName.value = ''
    characterDescription.value = ''
    await refreshState()
    await refreshCampaigns()
  } catch (err) {
    errorMessage.value = err instanceof Error ? err.message : String(err)
  } finally {
    loading.addCharacter = false
  }
}

async function removeCharacter(name: string) {
  const ok = window.confirm(`Remove character "${name}"?`)
  if (!ok) return

  loading.removeCharacter = true
  errorMessage.value = null
  try {
    await apiSend(`/api/adventure/characters?name=${encodeURIComponent(name)}`, 'DELETE')
    await refreshState()
    await refreshCampaigns()
  } catch (err) {
    errorMessage.value = err instanceof Error ? err.message : String(err)
  } finally {
    loading.removeCharacter = false
  }
}

async function addThread() {
  const name = threadName.value.trim()
  if (!name) return

  loading.addThread = true
  errorMessage.value = null
  try {
    await apiSend('/api/adventure/threads', 'POST', {
      name,
      description: threadDescription.value.trim() || null,
    })
    threadName.value = ''
    threadDescription.value = ''
    await refreshState()
    await refreshCampaigns()
  } catch (err) {
    errorMessage.value = err instanceof Error ? err.message : String(err)
  } finally {
    loading.addThread = false
  }
}

async function closeThread(name: string) {
  loading.closeThread = true
  errorMessage.value = null
  try {
    await apiSend(`/api/adventure/threads/close?name=${encodeURIComponent(name)}`, 'POST')
    await refreshState()
    await refreshCampaigns()
  } catch (err) {
    errorMessage.value = err instanceof Error ? err.message : String(err)
  } finally {
    loading.closeThread = false
  }
}

async function reopenThread(name: string) {
  loading.reopenThread = true
  errorMessage.value = null
  try {
    await apiSend(`/api/adventure/threads/reopen?name=${encodeURIComponent(name)}`, 'POST')
    await refreshState()
    await refreshCampaigns()
  } catch (err) {
    errorMessage.value = err instanceof Error ? err.message : String(err)
  } finally {
    loading.reopenThread = false
  }
}

async function saveJournal() {
  if (!currentCampaignId.value) return
  loading.saveJournal = true
  errorMessage.value = null
  try {
    await apiSend<{ saved: boolean }>('/api/journal', 'PUT', { content: journal.value })
  } catch (err) {
    errorMessage.value = err instanceof Error ? err.message : String(err)
  } finally {
    loading.saveJournal = false
  }
}

onMounted(() => {
  void refreshAll()
})
</script>

<template>
  <div class="min-h-full bg-gradient-to-br from-stone-50 via-amber-50 to-sky-50 text-slate-900">
    <div class="pointer-events-none fixed inset-0 opacity-[0.35] [background:radial-gradient(900px_circle_at_20%_10%,rgba(14,165,233,0.18),transparent_55%),radial-gradient(700px_circle_at_80%_20%,rgba(245,158,11,0.16),transparent_55%),radial-gradient(900px_circle_at_50%_100%,rgba(120,113,108,0.16),transparent_55%)]" />

    <header class="relative mx-auto max-w-6xl px-4 pt-10">
      <div class="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <div class="inline-flex items-center gap-3">
            <div class="h-10 w-10 rounded-2xl bg-slate-900 text-stone-50 shadow-sm ring-1 ring-white/40">
              <div class="grid h-full w-full place-items-center text-sm font-semibold">SF</div>
            </div>
            <div>
              <h1 class="text-2xl font-semibold tracking-tight">SoloForge</h1>
              <p class="text-sm text-slate-600">Web UI (single-user) for Mythic GME 2e workflows</p>
            </div>
          </div>
        </div>

        <div class="flex items-center gap-2">
          <div
            class="inline-flex items-center gap-2 rounded-full border border-white/60 bg-white/70 px-3 py-1 text-xs text-slate-700 shadow-sm backdrop-blur"
          >
            <span
              class="h-2 w-2 rounded-full"
              :class="
                apiOnline === null
                  ? 'bg-slate-300'
                  : apiOnline
                    ? 'bg-emerald-500'
                    : 'bg-rose-500'
              "
            />
            <span v-if="apiOnline === null">Checking API…</span>
            <span v-else-if="apiOnline">API online</span>
            <span v-else>API offline (start `dotnet run --project src/SoloForge.Api`)</span>
          </div>

          <button
            class="rounded-full border border-white/60 bg-white/70 px-4 py-1.5 text-xs font-medium text-slate-800 shadow-sm backdrop-blur transition hover:bg-white"
            type="button"
            @click="refreshAll"
            :disabled="isBusy"
          >
            Refresh
          </button>
        </div>
      </div>

      <div v-if="errorMessage" class="mt-6 rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-900">
        {{ errorMessage }}
      </div>
    </header>

    <main class="relative mx-auto max-w-6xl px-4 pb-12 pt-8">
      <div class="grid grid-cols-1 gap-6 lg:grid-cols-12">
        <section class="lg:col-span-5">
          <div class="rounded-3xl border border-white/60 bg-white/70 p-5 shadow-sm backdrop-blur">
            <div class="flex items-start justify-between gap-3">
              <div>
                <div class="text-xs font-medium tracking-wide text-slate-500">Current campaign</div>
                <div class="mt-1 text-lg font-semibold">{{ currentCampaignName }}</div>
                <div v-if="state?.currentCampaign" class="mt-1 text-xs text-slate-600">
                  Last played: {{ formatDate(state.currentCampaign.lastPlayed) }}
                </div>
              </div>
              <div class="rounded-2xl border border-white/70 bg-white px-3 py-2 text-center shadow-sm">
                <div class="text-[11px] font-medium text-slate-500">Chaos</div>
                <div class="mt-0.5 text-lg font-semibold tabular-nums">{{ state?.session.chaos ?? '—' }}</div>
              </div>
            </div>

            <div class="mt-5 grid grid-cols-1 gap-3 sm:grid-cols-5">
              <div class="sm:col-span-3">
                <label class="block text-xs font-medium text-slate-600">Set chaos factor (1–9)</label>
                <input
                  v-model.number="chaosDraft"
                  type="number"
                  min="1"
                  max="9"
                  class="mt-1 w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm shadow-sm outline-none ring-0 transition focus:border-slate-300 focus:shadow"
                />
              </div>
              <div class="sm:col-span-2 sm:flex sm:items-end">
                <button
                  type="button"
                  class="w-full rounded-xl bg-slate-900 px-3 py-2 text-sm font-medium text-white shadow-sm transition hover:bg-slate-800 disabled:opacity-50"
                  :disabled="loading.updateSession || apiOnline === false"
                  @click="updateSession"
                >
                  Apply
                </button>
              </div>
            </div>

            <div class="mt-3 grid grid-cols-1 gap-3 sm:grid-cols-2">
              <div>
                <label class="block text-xs font-medium text-slate-600">Engine</label>
                <input
                  v-model="engineDraft"
                  class="mt-1 w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm shadow-sm outline-none transition focus:border-slate-300 focus:shadow"
                />
              </div>
              <div>
                <label class="block text-xs font-medium text-slate-600">Theme</label>
                <select
                  v-if="themes.length > 0"
                  v-model="themeDraft"
                  class="mt-1 w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm shadow-sm outline-none transition focus:border-slate-300 focus:shadow"
                >
                  <option v-for="t in themes" :key="t.name" :value="t.name">{{ t.name }}</option>
                </select>
                <input
                  v-else
                  v-model="themeDraft"
                  class="mt-1 w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm shadow-sm outline-none transition focus:border-slate-300 focus:shadow"
                />
              </div>
            </div>
          </div>

          <div class="mt-6 rounded-3xl border border-white/60 bg-white/70 p-5 shadow-sm backdrop-blur">
            <div class="flex items-center justify-between">
              <h2 class="text-sm font-semibold tracking-tight">Campaigns</h2>
              <div class="text-xs text-slate-500">{{ campaigns.length }} total</div>
            </div>

            <div class="mt-4 flex gap-2">
              <input
                v-model="newCampaignName"
                placeholder="New campaign name"
                class="w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm shadow-sm outline-none transition focus:border-slate-300 focus:shadow"
                @keydown.enter="createCampaign"
              />
              <button
                type="button"
                class="shrink-0 rounded-xl bg-amber-500 px-4 py-2 text-sm font-semibold text-amber-950 shadow-sm transition hover:bg-amber-400 disabled:opacity-50"
                :disabled="loading.createCampaign || !newCampaignName.trim() || apiOnline === false"
                @click="createCampaign"
              >
                Create
              </button>
            </div>

            <div class="mt-4 max-h-[320px] overflow-auto rounded-2xl border border-white/70 bg-white">
              <div v-if="loading.campaigns" class="p-4 text-sm text-slate-600">Loading campaigns…</div>
              <div v-else-if="campaigns.length === 0" class="p-4 text-sm text-slate-600">No campaigns found.</div>
              <ul v-else class="divide-y divide-slate-100">
                <li
                  v-for="c in campaigns"
                  :key="c.id"
                  class="flex items-center justify-between gap-3 p-3"
                >
                  <div class="min-w-0">
                    <div class="flex items-center gap-2">
                      <div class="truncate text-sm font-semibold">
                        {{ c.name }}
                      </div>
                      <span
                        v-if="c.id === currentCampaignId"
                        class="rounded-full bg-emerald-50 px-2 py-0.5 text-[11px] font-semibold text-emerald-700"
                      >
                        current
                      </span>
                    </div>
                    <div class="mt-0.5 text-xs text-slate-500">
                      Last played: {{ formatDate(c.lastPlayed) }} · Entries: {{ c.historyCount }}
                    </div>
                  </div>
                  <div class="flex items-center gap-2">
                    <button
                      type="button"
                      class="rounded-xl border border-slate-200 bg-white px-3 py-1.5 text-xs font-semibold text-slate-800 shadow-sm transition hover:bg-slate-50 disabled:opacity-50"
                      :disabled="loading.loadCampaign || c.id === currentCampaignId || apiOnline === false"
                      @click="loadCampaign(c.id)"
                    >
                      Load
                    </button>
                    <button
                      type="button"
                      class="rounded-xl border border-rose-200 bg-rose-50 px-3 py-1.5 text-xs font-semibold text-rose-800 shadow-sm transition hover:bg-rose-100 disabled:opacity-50"
                      :disabled="loading.deleteCampaign || campaigns.length <= 1 || apiOnline === false"
                      @click="deleteCampaign(c.id)"
                    >
                      Delete
                    </button>
                  </div>
                </li>
              </ul>
            </div>
          </div>

          <div class="mt-6 rounded-3xl border border-white/60 bg-white/70 p-5 shadow-sm backdrop-blur">
            <div class="flex items-center justify-between">
              <h2 class="text-sm font-semibold tracking-tight">Fate check</h2>
              <div v-if="state" class="text-xs text-slate-500">Chaos {{ state.session.chaos }}</div>
            </div>

            <div class="mt-4 grid grid-cols-1 gap-3">
              <div>
                <label class="block text-xs font-medium text-slate-600">Odds</label>
                <select
                  v-model="fateOdds"
                  class="mt-1 w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm shadow-sm outline-none transition focus:border-slate-300 focus:shadow"
                >
                  <option v-for="o in oddsOptions" :key="o.value" :value="o.value">{{ o.label }}</option>
                </select>
              </div>
              <div>
                <label class="block text-xs font-medium text-slate-600">Question (optional)</label>
                <input
                  v-model="fateQuestion"
                  placeholder="Does the guard notice me?"
                  class="mt-1 w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm shadow-sm outline-none transition focus:border-slate-300 focus:shadow"
                  @keydown.enter="runFateCheck"
                />
              </div>

              <button
                type="button"
                class="rounded-xl bg-slate-900 px-4 py-2 text-sm font-semibold text-white shadow-sm transition hover:bg-slate-800 disabled:opacity-50"
                :disabled="loading.fateCheck || apiOnline === false"
                @click="runFateCheck"
              >
                Roll
              </button>
            </div>

            <div v-if="fateResult" class="mt-4 rounded-2xl border border-white/70 bg-white p-4">
              <div class="flex items-start justify-between gap-3">
                <div>
                  <div class="text-xs font-medium text-slate-500">Result</div>
                  <div
                    class="mt-1 text-lg font-semibold"
                    :class="
                      fateResult.fate.result.includes('Yes')
                        ? 'text-emerald-700'
                        : fateResult.fate.result.includes('No')
                          ? 'text-rose-700'
                          : 'text-slate-900'
                    "
                  >
                    {{ fateResult.fate.result }}
                  </div>
                  <div class="mt-1 text-xs text-slate-600">Roll {{ fateResult.fate.roll }} · Odds {{ fateResult.odds }}</div>
                </div>
                <div class="rounded-xl bg-slate-50 px-3 py-2 text-center">
                  <div class="text-[11px] font-medium text-slate-500">Chaos</div>
                  <div class="mt-0.5 text-base font-semibold tabular-nums">{{ fateResult.chaos }}</div>
                </div>
              </div>

              <div v-if="fateResult.randomEvent" class="mt-4 rounded-xl border border-amber-200 bg-amber-50 p-3">
                <div class="text-xs font-semibold text-amber-900">Random event</div>
                <div class="mt-1 text-sm font-semibold text-slate-900">
                  {{ fateResult.randomEvent.eventFocus }}: {{ fateResult.randomEvent.eventAction }}
                </div>
                <div v-if="fateResult.randomEvent.selectedCharacter" class="mt-1 text-xs text-slate-700">
                  Character: {{ fateResult.randomEvent.selectedCharacter }}
                </div>
                <div v-if="fateResult.randomEvent.selectedThread" class="mt-1 text-xs text-slate-700">
                  Thread: {{ fateResult.randomEvent.selectedThread }}
                </div>
                <div v-if="fateResult.randomEvent.listWasEmpty" class="mt-1 text-xs text-slate-700">
                  (List was empty)
                </div>
              </div>
            </div>
          </div>

          <div class="mt-6 rounded-3xl border border-white/60 bg-white/70 p-5 shadow-sm backdrop-blur">
            <div class="flex items-center justify-between">
              <h2 class="text-sm font-semibold tracking-tight">Scene check</h2>
              <div v-if="state" class="text-xs text-slate-500">Chaos {{ state.session.chaos }}</div>
            </div>

            <div class="mt-4 grid grid-cols-1 gap-3">
              <div>
                <label class="block text-xs font-medium text-slate-600">Scene context (optional)</label>
                <input
                  v-model="sceneContext"
                  placeholder="What is the scene setup?"
                  class="mt-1 w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm shadow-sm outline-none transition focus:border-slate-300 focus:shadow"
                  @keydown.enter="runSceneCheck"
                />
              </div>

              <button
                type="button"
                class="rounded-xl bg-slate-900 px-4 py-2 text-sm font-semibold text-white shadow-sm transition hover:bg-slate-800 disabled:opacity-50"
                :disabled="loading.sceneCheck || apiOnline === false"
                @click="runSceneCheck"
              >
                Check scene
              </button>
            </div>

            <div v-if="sceneResult" class="mt-4 rounded-2xl border border-white/70 bg-white p-4">
              <div class="flex items-start justify-between gap-3">
                <div>
                  <div class="text-xs font-medium text-slate-500">Result</div>
                  <div
                    class="mt-1 text-lg font-semibold"
                    :class="
                      sceneResult.scene.result === 'Normal Scene'
                        ? 'text-emerald-700'
                        : sceneResult.scene.result.includes('Altered')
                          ? 'text-amber-800'
                          : sceneResult.scene.result.includes('Interrupt')
                            ? 'text-rose-700'
                            : 'text-slate-900'
                    "
                  >
                    {{ sceneResult.scene.result }}
                  </div>
                  <div class="mt-1 text-xs text-slate-600">
                    Roll {{ sceneResult.scene.roll }} · Chaos {{ sceneResult.chaos }}
                  </div>
                </div>
              </div>

              <div v-if="sceneResult.scene.sceneAdjustment" class="mt-4 rounded-xl border border-amber-200 bg-amber-50 p-3">
                <div class="text-xs font-semibold text-amber-900">Scene adjustment</div>
                <div class="mt-1 text-sm font-semibold text-slate-900">{{ sceneResult.scene.sceneAdjustment }}</div>
              </div>

              <div v-if="sceneResult.scene.randomEvent" class="mt-4 rounded-xl border border-sky-200 bg-sky-50 p-3">
                <div class="text-xs font-semibold text-sky-900">Random event</div>
                <div class="mt-1 text-sm font-semibold text-slate-900">
                  {{ sceneResult.scene.randomEvent.eventFocus }}: {{ sceneResult.scene.randomEvent.eventAction }}
                </div>
                <div v-if="sceneResult.scene.randomEvent.selectedCharacter" class="mt-1 text-xs text-slate-700">
                  Character: {{ sceneResult.scene.randomEvent.selectedCharacter }}
                </div>
                <div v-if="sceneResult.scene.randomEvent.selectedThread" class="mt-1 text-xs text-slate-700">
                  Thread: {{ sceneResult.scene.randomEvent.selectedThread }}
                </div>
                <div v-if="sceneResult.scene.randomEvent.listWasEmpty" class="mt-1 text-xs text-slate-700">
                  (List was empty)
                </div>
              </div>
            </div>
          </div>

          <div class="mt-6 rounded-3xl border border-white/60 bg-white/70 p-5 shadow-sm backdrop-blur">
            <div class="flex items-center justify-between">
              <h2 class="text-sm font-semibold tracking-tight">Random event</h2>
              <button
                type="button"
                class="rounded-xl bg-slate-900 px-3 py-1.5 text-xs font-semibold text-white shadow-sm transition hover:bg-slate-800 disabled:opacity-50"
                :disabled="loading.randomEvent || apiOnline === false"
                @click="runRandomEvent"
              >
                Roll
              </button>
            </div>

            <div v-if="randomResult" class="mt-4 rounded-2xl border border-white/70 bg-white p-4">
              <div class="text-xs font-medium text-slate-500">Focus</div>
              <div class="mt-1 text-base font-semibold text-slate-900">{{ randomResult.eventFocus }}</div>
              <div class="mt-3 text-xs font-medium text-slate-500">Action</div>
              <div class="mt-1 text-base font-semibold text-slate-900">{{ randomResult.eventAction }}</div>

              <div v-if="randomResult.selectedCharacter" class="mt-3 text-sm text-slate-700">
                Character: <span class="font-semibold text-slate-900">{{ randomResult.selectedCharacter }}</span>
              </div>
              <div v-if="randomResult.selectedThread" class="mt-1 text-sm text-slate-700">
                Thread: <span class="font-semibold text-slate-900">{{ randomResult.selectedThread }}</span>
              </div>
              <div v-if="randomResult.listWasEmpty" class="mt-3 text-sm text-slate-700">(List was empty)</div>
              <div v-if="randomResult.isNewNpc" class="mt-3 text-sm font-semibold text-amber-900">New NPC: add them to your character list.</div>
            </div>

            <div v-if="randomResult?.isNewNpc" class="mt-4 rounded-2xl border border-amber-200 bg-amber-50 p-4">
              <div class="text-xs font-semibold text-amber-900">Add NPC</div>
              <div class="mt-3 grid grid-cols-1 gap-3 sm:grid-cols-5">
                <div class="sm:col-span-3">
                  <label class="block text-xs font-medium text-slate-600">Name</label>
                  <input
                    v-model="newNpcName"
                    class="mt-1 w-full rounded-xl border border-amber-200 bg-white px-3 py-2 text-sm shadow-sm outline-none transition focus:border-amber-300 focus:shadow"
                    placeholder="NPC name"
                    @keydown.enter="addNpcFromRandomEvent"
                  />
                </div>
                <div class="sm:col-span-2">
                  <label class="block text-xs font-medium text-slate-600">Description</label>
                  <input
                    v-model="newNpcDescription"
                    class="mt-1 w-full rounded-xl border border-amber-200 bg-white px-3 py-2 text-sm shadow-sm outline-none transition focus:border-amber-300 focus:shadow"
                    placeholder="Optional"
                  />
                </div>
              </div>
              <button
                type="button"
                class="mt-3 w-full rounded-xl bg-amber-600 px-3 py-2 text-sm font-semibold text-white shadow-sm transition hover:bg-amber-500 disabled:opacity-50"
                :disabled="loading.addCharacter || !newNpcName.trim() || apiOnline === false"
                @click="addNpcFromRandomEvent"
              >
                Add NPC
              </button>
            </div>
          </div>

          <div class="mt-6 rounded-3xl border border-white/60 bg-white/70 p-5 shadow-sm backdrop-blur">
            <div class="flex items-center justify-between">
              <h2 class="text-sm font-semibold tracking-tight">Meaning</h2>
              <div class="text-xs text-slate-500">Action/Description/Tables/Fusion/Quick Sets</div>
            </div>

            <div class="mt-4 flex flex-wrap gap-2">
              <button
                type="button"
                class="rounded-full px-3 py-1.5 text-xs font-semibold shadow-sm transition"
                :class="meaningMode === 'action' ? 'bg-slate-900 text-white' : 'border border-slate-200 bg-white text-slate-800 hover:bg-slate-50'"
                @click="meaningMode = 'action'"
              >
                Action
              </button>
              <button
                type="button"
                class="rounded-full px-3 py-1.5 text-xs font-semibold shadow-sm transition"
                :class="meaningMode === 'description' ? 'bg-slate-900 text-white' : 'border border-slate-200 bg-white text-slate-800 hover:bg-slate-50'"
                @click="meaningMode = 'description'"
              >
                Description
              </button>
              <button
                type="button"
                class="rounded-full px-3 py-1.5 text-xs font-semibold shadow-sm transition"
                :class="meaningMode === 'table' ? 'bg-slate-900 text-white' : 'border border-slate-200 bg-white text-slate-800 hover:bg-slate-50'"
                @click="meaningMode = 'table'"
              >
                Table
              </button>
              <button
                type="button"
                class="rounded-full px-3 py-1.5 text-xs font-semibold shadow-sm transition"
                :class="meaningMode === 'fusion' ? 'bg-slate-900 text-white' : 'border border-slate-200 bg-white text-slate-800 hover:bg-slate-50'"
                @click="meaningMode = 'fusion'"
              >
                Fusion
              </button>
              <button
                type="button"
                class="rounded-full px-3 py-1.5 text-xs font-semibold shadow-sm transition"
                :class="meaningMode === 'quickSet' ? 'bg-slate-900 text-white' : 'border border-slate-200 bg-white text-slate-800 hover:bg-slate-50'"
                @click="meaningMode = 'quickSet'"
              >
                Quick set
              </button>
            </div>

            <div class="mt-4 grid grid-cols-1 gap-3">
              <div>
                <label class="block text-xs font-medium text-slate-600">Context (optional)</label>
                <input
                  v-model="meaningContext"
                  placeholder="What are you trying to understand?"
                  class="mt-1 w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm shadow-sm outline-none transition focus:border-slate-300 focus:shadow"
                  @keydown.enter="runMeaning"
                />
              </div>

              <div v-if="meaningMode === 'table'">
                <label class="block text-xs font-medium text-slate-600">Table</label>
                <select
                  v-model="meaningTableId"
                  class="mt-1 w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm shadow-sm outline-none transition focus:border-slate-300 focus:shadow"
                >
                  <optgroup v-for="g in tableGroups" :key="g.label" :label="g.label">
                    <option v-for="t in g.items" :key="t.id" :value="t.id">{{ t.displayName }}</option>
                  </optgroup>
                </select>
              </div>

              <div v-else-if="meaningMode === 'fusion'" class="grid grid-cols-1 gap-3 sm:grid-cols-2">
                <div>
                  <label class="block text-xs font-medium text-slate-600">Table 1</label>
                  <select
                    v-model="meaningFusionTable1"
                    class="mt-1 w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm shadow-sm outline-none transition focus:border-slate-300 focus:shadow"
                  >
                    <optgroup v-for="g in tableGroups" :key="g.label" :label="g.label">
                      <option v-for="t in g.items" :key="t.id" :value="t.id">{{ t.displayName }}</option>
                    </optgroup>
                  </select>
                </div>
                <div>
                  <label class="block text-xs font-medium text-slate-600">Table 2</label>
                  <select
                    v-model="meaningFusionTable2"
                    class="mt-1 w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm shadow-sm outline-none transition focus:border-slate-300 focus:shadow"
                  >
                    <optgroup v-for="g in tableGroups" :key="g.label" :label="g.label">
                      <option v-for="t in g.items" :key="t.id" :value="t.id">{{ t.displayName }}</option>
                    </optgroup>
                  </select>
                </div>
              </div>

              <div v-else-if="meaningMode === 'quickSet'">
                <label class="block text-xs font-medium text-slate-600">Quick set</label>
                <select
                  v-model="meaningQuickSetId"
                  class="mt-1 w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm shadow-sm outline-none transition focus:border-slate-300 focus:shadow"
                >
                  <option v-for="q in quickSets" :key="q.id" :value="q.id">{{ q.name }}</option>
                </select>
              </div>

              <button
                type="button"
                class="rounded-xl bg-slate-900 px-4 py-2 text-sm font-semibold text-white shadow-sm transition hover:bg-slate-800 disabled:opacity-50"
                :disabled="loading.meaning || apiOnline === false"
                @click="runMeaning"
              >
                Roll
              </button>
            </div>

            <div v-if="meaningResult" class="mt-4 rounded-2xl border border-white/70 bg-white p-4">
              <div class="text-xs font-medium text-slate-500">Result</div>
              <div class="mt-1 text-lg font-semibold text-slate-900">{{ meaningResult.combined }}</div>
              <div class="mt-1 text-xs text-slate-600">
                <span v-if="meaningMeta">{{ meaningMeta }}</span>
                <span v-else>{{ meaningResult.tableName }}</span>
              </div>
              <div class="mt-3 grid grid-cols-1 gap-2 sm:grid-cols-2">
                <div class="rounded-xl bg-slate-50 px-3 py-2">
                  <div class="text-[11px] font-medium text-slate-500">Word 1</div>
                  <div class="mt-0.5 text-sm font-semibold text-slate-900">{{ meaningResult.word1 }}</div>
                </div>
                <div class="rounded-xl bg-slate-50 px-3 py-2">
                  <div class="text-[11px] font-medium text-slate-500">Word 2</div>
                  <div class="mt-0.5 text-sm font-semibold text-slate-900">{{ meaningResult.word2 }}</div>
                </div>
              </div>
            </div>

            <div v-if="quickSetResult" class="mt-4 rounded-2xl border border-white/70 bg-white p-4">
              <div class="text-xs font-medium text-slate-500">Quick set</div>
              <div class="mt-1 text-base font-semibold text-slate-900">{{ quickSetResult.quickSet.name }}</div>
              <div class="mt-1 text-xs text-slate-600">{{ quickSetResult.quickSet.description }}</div>

              <div class="mt-4 rounded-xl border border-slate-200 bg-slate-50 p-3">
                <div v-for="r in quickSetResult.results" :key="r.label" class="py-1 text-sm">
                  <span class="font-semibold text-slate-900">{{ r.label }}:</span>
                  <span class="text-slate-800"> {{ r.combined }}</span>
                </div>
              </div>
            </div>
          </div>

          <div class="mt-6 rounded-3xl border border-white/60 bg-white/70 p-5 shadow-sm backdrop-blur">
            <div class="flex items-center justify-between">
              <h2 class="text-sm font-semibold tracking-tight">Dice roller</h2>
              <div class="text-xs text-slate-500">2d6+1, d20, 1d8-2</div>
            </div>

            <div class="mt-4 grid grid-cols-1 gap-3">
              <div>
                <label class="block text-xs font-medium text-slate-600">Expression</label>
                <input
                  v-model="diceExpression"
                  placeholder="2d6+1"
                  class="mt-1 w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm shadow-sm outline-none transition focus:border-slate-300 focus:shadow"
                  @keydown.enter="rollDice()"
                />
              </div>

              <div class="flex flex-wrap gap-2">
                <button
                  v-for="die in ['d4','d6','d8','d10','d12','d20','d100']"
                  :key="die"
                  type="button"
                  class="rounded-full border border-slate-200 bg-white px-3 py-1 text-xs font-semibold text-slate-800 shadow-sm transition hover:bg-slate-50"
                  @click="rollDice('1' + die)"
                >
                  {{ die }}
                </button>
              </div>

              <button
                type="button"
                class="rounded-xl bg-slate-900 px-4 py-2 text-sm font-semibold text-white shadow-sm transition hover:bg-slate-800 disabled:opacity-50"
                :disabled="loading.diceRoll || apiOnline === false"
                @click="rollDice()"
              >
                Roll
              </button>
            </div>

            <div v-if="diceResult" class="mt-4 rounded-2xl border border-white/70 bg-white p-4">
              <div class="text-xs font-medium text-slate-500">Result</div>
              <div class="mt-1 text-lg font-semibold text-slate-900">{{ diceResult.roll.summary }}</div>
              <div v-if="diceResult.breakdown" class="mt-2 rounded-xl bg-slate-50 px-3 py-2 font-mono text-[12px] leading-5 text-slate-800">
                {{ diceResult.breakdown }}
              </div>
            </div>
          </div>

          <div class="mt-6 rounded-3xl border border-white/60 bg-white/70 p-5 shadow-sm backdrop-blur">
            <div class="flex items-center justify-between">
              <h2 class="text-sm font-semibold tracking-tight">Adventure lists</h2>
              <div v-if="state" class="text-xs text-slate-500">
                {{ state.adventure.characters.length }} characters · {{ state.adventure.activeThreads.length }} active threads ·
                {{ state.adventure.closedThreads.length }} closed
              </div>
            </div>

            <div class="mt-4 grid grid-cols-1 gap-4 sm:grid-cols-2">
              <div>
                <div class="text-xs font-semibold text-slate-700">Characters</div>
                <div class="mt-2 max-h-[220px] overflow-auto rounded-2xl border border-slate-200 bg-white">
                  <div v-if="!state || state.adventure.characters.length === 0" class="p-3 text-sm text-slate-600">
                    (No characters)
                  </div>
                  <ul v-else class="divide-y divide-slate-100">
                    <li v-for="c in state.adventure.characters" :key="c.name" class="flex items-center justify-between gap-3 p-3">
                      <div class="min-w-0">
                        <div class="truncate text-sm font-semibold text-slate-900">{{ c.name }}</div>
                        <div v-if="c.description" class="mt-0.5 truncate text-xs text-slate-600">{{ c.description }}</div>
                      </div>
                      <button
                        type="button"
                        class="rounded-xl border border-rose-200 bg-rose-50 px-3 py-1.5 text-xs font-semibold text-rose-800 shadow-sm transition hover:bg-rose-100 disabled:opacity-50"
                        :disabled="loading.removeCharacter || apiOnline === false"
                        @click="removeCharacter(c.name)"
                      >
                        Remove
                      </button>
                    </li>
                  </ul>
                </div>

                <div class="mt-3 grid grid-cols-1 gap-2">
                  <input
                    v-model="characterName"
                    placeholder="Character name"
                    class="w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm shadow-sm outline-none transition focus:border-slate-300 focus:shadow"
                    @keydown.enter="addCharacter"
                  />
                  <input
                    v-model="characterDescription"
                    placeholder="Description (optional)"
                    class="w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm shadow-sm outline-none transition focus:border-slate-300 focus:shadow"
                    @keydown.enter="addCharacter"
                  />
                  <button
                    type="button"
                    class="rounded-xl bg-slate-900 px-3 py-2 text-sm font-semibold text-white shadow-sm transition hover:bg-slate-800 disabled:opacity-50"
                    :disabled="loading.addCharacter || !characterName.trim() || apiOnline === false"
                    @click="addCharacter"
                  >
                    Add character
                  </button>
                </div>
              </div>

              <div>
                <div class="text-xs font-semibold text-slate-700">Threads</div>
                <div class="mt-2 rounded-2xl border border-slate-200 bg-white">
                  <div class="p-3">
                    <div class="text-[11px] font-semibold text-slate-500">Active</div>
                    <div v-if="!state || state.adventure.activeThreads.length === 0" class="mt-1 text-sm text-slate-600">
                      (No active threads)
                    </div>
                    <ul v-else class="mt-2 space-y-2">
                      <li v-for="t in state.adventure.activeThreads" :key="t.name" class="flex items-center justify-between gap-3">
                        <div class="min-w-0">
                          <div class="truncate text-sm font-semibold text-slate-900">{{ t.name }}</div>
                          <div v-if="t.description" class="mt-0.5 truncate text-xs text-slate-600">{{ t.description }}</div>
                        </div>
                        <button
                          type="button"
                          class="rounded-xl border border-amber-200 bg-amber-50 px-3 py-1.5 text-xs font-semibold text-amber-900 shadow-sm transition hover:bg-amber-100 disabled:opacity-50"
                          :disabled="loading.closeThread || apiOnline === false"
                          @click="closeThread(t.name)"
                        >
                          Resolve
                        </button>
                      </li>
                    </ul>

                    <div class="mt-4 text-[11px] font-semibold text-slate-500">Closed</div>
                    <div v-if="!state || state.adventure.closedThreads.length === 0" class="mt-1 text-sm text-slate-600">
                      (No closed threads)
                    </div>
                    <ul v-else class="mt-2 space-y-2">
                      <li v-for="t in state.adventure.closedThreads" :key="t.name" class="flex items-center justify-between gap-3">
                        <div class="min-w-0">
                          <div class="truncate text-sm font-semibold text-slate-900">{{ t.name }}</div>
                          <div v-if="t.description" class="mt-0.5 truncate text-xs text-slate-600">{{ t.description }}</div>
                        </div>
                        <button
                          type="button"
                          class="rounded-xl border border-slate-200 bg-white px-3 py-1.5 text-xs font-semibold text-slate-800 shadow-sm transition hover:bg-slate-50 disabled:opacity-50"
                          :disabled="loading.reopenThread || apiOnline === false"
                          @click="reopenThread(t.name)"
                        >
                          Reopen
                        </button>
                      </li>
                    </ul>
                  </div>
                </div>

                <div class="mt-3 grid grid-cols-1 gap-2">
                  <input
                    v-model="threadName"
                    placeholder="Thread name"
                    class="w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm shadow-sm outline-none transition focus:border-slate-300 focus:shadow"
                    @keydown.enter="addThread"
                  />
                  <input
                    v-model="threadDescription"
                    placeholder="Description (optional)"
                    class="w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm shadow-sm outline-none transition focus:border-slate-300 focus:shadow"
                    @keydown.enter="addThread"
                  />
                  <button
                    type="button"
                    class="rounded-xl bg-slate-900 px-3 py-2 text-sm font-semibold text-white shadow-sm transition hover:bg-slate-800 disabled:opacity-50"
                    :disabled="loading.addThread || !threadName.trim() || apiOnline === false"
                    @click="addThread"
                  >
                    Add thread
                  </button>
                </div>
              </div>
            </div>
          </div>

          <div class="mt-6 rounded-3xl border border-white/60 bg-white/70 p-5 shadow-sm backdrop-blur">
            <div class="flex items-center justify-between">
              <h2 class="text-sm font-semibold tracking-tight">History</h2>
              <button
                type="button"
                class="rounded-xl border border-slate-200 bg-white px-3 py-1.5 text-xs font-semibold text-slate-800 shadow-sm transition hover:bg-slate-50 disabled:opacity-50"
                :disabled="loading.history || apiOnline === false"
                @click="refreshHistory"
              >
                Refresh
              </button>
            </div>

            <div class="mt-4 max-h-[300px] overflow-auto rounded-2xl border border-white/70 bg-white">
              <div v-if="loading.history" class="p-4 text-sm text-slate-600">Loading history…</div>
              <div v-else-if="history.length === 0" class="p-4 text-sm text-slate-600">No history yet.</div>
              <ul v-else class="divide-y divide-slate-100">
                <li v-for="e in history" :key="e.id" class="p-3">
                  <div class="flex items-start justify-between gap-3">
                    <div class="min-w-0">
                      <div class="text-xs font-semibold text-slate-700">{{ e.type }}</div>
                      <div class="mt-0.5 text-sm font-semibold text-slate-900">{{ e.result }}</div>
                      <div v-if="e.context" class="mt-0.5 text-xs text-slate-600">{{ e.context }}</div>
                      <div v-if="e.details" class="mt-1 text-xs text-slate-500">{{ e.details }}</div>
                    </div>
                    <div class="shrink-0 text-[11px] text-slate-500">{{ formatDate(e.timestamp) }}</div>
                  </div>
                </li>
              </ul>
            </div>
          </div>
        </section>

        <section class="lg:col-span-7">
          <div class="rounded-3xl border border-white/60 bg-white/70 p-5 shadow-sm backdrop-blur">
            <div class="flex items-center justify-between">
              <h2 class="text-sm font-semibold tracking-tight">Journal</h2>
              <div class="flex items-center gap-2">
                <button
                  type="button"
                  class="rounded-xl border border-slate-200 bg-white px-3 py-1.5 text-xs font-semibold text-slate-800 shadow-sm transition hover:bg-slate-50 disabled:opacity-50"
                  :disabled="loading.journal || apiOnline === false"
                  @click="refreshJournal"
                >
                  Reload
                </button>
                <button
                  type="button"
                  class="rounded-xl bg-emerald-600 px-3 py-1.5 text-xs font-semibold text-white shadow-sm transition hover:bg-emerald-500 disabled:opacity-50"
                  :disabled="loading.saveJournal || !currentCampaignId || apiOnline === false"
                  @click="saveJournal"
                >
                  Save
                </button>
              </div>
            </div>

            <div class="mt-4">
              <textarea
                v-model="journal"
                class="h-[55vh] min-h-[420px] w-full resize-none rounded-2xl border border-slate-200 bg-white p-4 font-mono text-[13px] leading-5 text-slate-900 shadow-sm outline-none transition focus:border-slate-300 focus:shadow lg:h-[calc(100vh-18rem)]"
                :placeholder="currentCampaignId ? 'Journal markdown…' : 'Load or create a campaign first.'"
                :disabled="!currentCampaignId"
              />
              <div class="mt-2 text-xs text-slate-500">
                Saved in your local `saves/` folder as markdown. This is plain text; rendering comes later.
              </div>
            </div>
          </div>
        </section>
      </div>
    </main>
  </div>
</template>
