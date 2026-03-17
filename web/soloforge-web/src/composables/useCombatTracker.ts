import { ref, computed, watch } from 'vue'
import type { Combatant, CombatTrackerState } from '../types'
import { useCampaign } from './useCampaign'

const STORAGE_KEY = 'soloforge-combat-tracker'

const DEFAULTS: CombatTrackerState = {
  combatants: [],
  activeCombatantId: null,
  round: 1,
  started: false,
}

function load(): CombatTrackerState {
  try {
    const stored = localStorage.getItem(STORAGE_KEY)
    if (stored) return { ...DEFAULTS, ...JSON.parse(stored) }
  } catch { /* ignore corrupted storage */ }
  return { ...DEFAULTS }
}

function createCombatant(): Combatant {
  return {
    id: crypto.randomUUID(),
    type: 'NPC',
    initiative: null,
    name: '',
    currentHp: 0,
    maxHp: 0,
    ac: null,
    conditions: '',
    status: 'active',
  }
}

const state = ref<CombatTrackerState>(load())

watch(state, (val) => {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(val))
  } catch { /* quota or private-mode errors */ }
}, { deep: true })

function findIndex(id: string): number {
  return state.value.combatants.findIndex(c => c.id === id)
}

function findLivingIndex(startIdx: number, direction: 1 | -1): number | null {
  const list = state.value.combatants
  const len = list.length
  if (len === 0) return null

  for (let i = 1; i <= len; i++) {
    const idx = ((startIdx + i * direction) % len + len) % len
    if (list[idx]!.status === 'active') return idx
  }
  return null
}

function checkAutoDead(combatant: Combatant) {
  if (combatant.currentHp <= 0 && combatant.status === 'active') {
    combatant.status = 'dead'
  }
}

export function useCombatTracker() {
  const combatants = computed(() => state.value.combatants)
  const round = computed(() => state.value.round)
  const started = computed(() => state.value.started)
  const activeCombatantId = computed(() => state.value.activeCombatantId)

  const activeCombatant = computed(() => {
    if (!state.value.activeCombatantId) return null
    return state.value.combatants.find(c => c.id === state.value.activeCombatantId) ?? null
  })

  function addCombatant(): Combatant {
    const c = createCombatant()
    state.value.combatants.push(c)
    return c
  }

  function removeCombatant(id: string) {
    const idx = findIndex(id)
    if (idx === -1) return

    const wasActive = state.value.activeCombatantId === id
    state.value.combatants.splice(idx, 1)

    if (wasActive) {
      // Advance forward from the removed position (idx now points to what was idx+1)
      const list = state.value.combatants
      let nextIdx: number | null = null
      if (list.length > 0) {
        const searchFrom = Math.min(idx, list.length - 1)
        nextIdx = findLivingIndex(searchFrom - 1, 1)
      }
      const nextCombatant = nextIdx !== null ? list[nextIdx]! : undefined
      if (nextCombatant) {
        state.value.activeCombatantId = nextCombatant.id
      } else {
        state.value.activeCombatantId = null
        state.value.started = false
      }
    }
  }

  function updateCombatant(id: string, patch: Partial<Omit<Combatant, 'id'>>) {
    const idx = findIndex(id)
    if (idx === -1) return
    const combatant = state.value.combatants[idx]!
    Object.assign(combatant, patch)
    if ('currentHp' in patch) {
      checkAutoDead(combatant)
    }
  }

  function rollInitiative(id: string) {
    const idx = findIndex(id)
    if (idx === -1) return
    state.value.combatants[idx]!.initiative = Math.floor(Math.random() * 20) + 1
  }

  function rollAllInitiative() {
    for (const c of state.value.combatants) {
      c.initiative = Math.floor(Math.random() * 20) + 1
    }
  }

  function sortByInitiative() {
    state.value.combatants.sort((a, b) => {
      if (a.initiative === null && b.initiative === null) return 0
      if (a.initiative === null) return 1
      if (b.initiative === null) return -1
      return b.initiative - a.initiative
    })
  }

  function adjustHp(id: string, delta: number) {
    const idx = findIndex(id)
    if (idx === -1) return
    const combatant = state.value.combatants[idx]!
    combatant.currentHp += delta
    checkAutoDead(combatant)
  }

  function nextTurn() {
    const list = state.value.combatants
    if (list.length === 0) return

    if (!state.value.started) {
      state.value.started = true
      const firstLiving = list.findIndex(c => c.status === 'active')
      if (firstLiving !== -1) {
        state.value.activeCombatantId = list[firstLiving]!.id
      }
      return
    }

    const currentIdx = state.value.activeCombatantId
      ? findIndex(state.value.activeCombatantId)
      : -1

    const nextIdx = findLivingIndex(currentIdx, 1)
    if (nextIdx === null) return

    // Check if we wrapped around (next index is at or before current)
    if (currentIdx !== -1 && nextIdx <= currentIdx) {
      state.value.round++
    }

    state.value.activeCombatantId = list[nextIdx]!.id
  }

  function prevTurn() {
    const list = state.value.combatants
    if (list.length === 0 || !state.value.started) return

    const currentIdx = state.value.activeCombatantId
      ? findIndex(state.value.activeCombatantId)
      : -1

    if (currentIdx === -1) return

    const prevIdx = findLivingIndex(currentIdx, -1)
    if (prevIdx === null) return

    // PRD: Back before the first row does nothing
    if (prevIdx >= currentIdx) return

    state.value.activeCombatantId = list[prevIdx]!.id
  }

  function setRound(n: number) {
    if (!Number.isFinite(n)) return
    state.value.round = Math.max(1, Math.floor(n))
  }

  function reorder(fromIdx: number, toIdx: number) {
    const list = state.value.combatants
    if (fromIdx < 0 || fromIdx >= list.length || toIdx < 0 || toIdx >= list.length) return
    const [item] = list.splice(fromIdx, 1) as [Combatant]
    list.splice(toIdx, 0, item)
  }

  function clearAll() {
    state.value.combatants = []
    state.value.activeCombatantId = null
    state.value.round = 1
    state.value.started = false
  }

  function getCharacterSuggestions(query: string): string[] {
    const { adventure } = useCampaign()
    const chars = adventure.value.characters
    if (!query) return chars.map(c => c.name)
    const lower = query.toLowerCase()
    return chars.filter(c => c.name.toLowerCase().includes(lower)).map(c => c.name)
  }

  return {
    state,
    combatants,
    activeCombatantId,
    activeCombatant,
    round,
    started,
    addCombatant,
    removeCombatant,
    updateCombatant,
    rollInitiative,
    rollAllInitiative,
    sortByInitiative,
    adjustHp,
    nextTurn,
    prevTurn,
    setRound,
    reorder,
    clearAll,
    getCharacterSuggestions,
  }
}
