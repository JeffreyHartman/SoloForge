import { describe, it, expect, vi, beforeEach } from 'vitest'

function createMockStorage(): Storage {
  const store = new Map<string, string>()
  return {
    getItem: (key: string) => store.get(key) ?? null,
    setItem: (key: string, value: string) => { store.set(key, value) },
    removeItem: (key: string) => { store.delete(key) },
    clear: () => { store.clear() },
    get length() { return store.size },
    key: (index: number) => [...store.keys()][index] ?? null,
  }
}

// Mock useCampaign for character suggestion tests
vi.mock('../useCampaign', () => ({
  useCampaign: () => ({
    adventure: {
      value: {
        characters: [
          { name: 'Gandalf', createdAt: '' },
          { name: 'Aragorn', createdAt: '' },
          { name: 'Gimli', createdAt: '' },
        ],
        activeThreads: [],
        closedThreads: [],
      },
    },
  }),
}))

describe('useCombatTracker', () => {
  let mockStorage: Storage

  beforeEach(() => {
    vi.resetModules()
    mockStorage = createMockStorage()
    vi.stubGlobal('localStorage', mockStorage)
  })

  // --- Initialization ---

  it('defaults to empty state', async () => {
    const { useCombatTracker } = await import('../useCombatTracker')
    const tracker = useCombatTracker()
    expect(tracker.combatants.value).toEqual([])
    expect(tracker.round.value).toBe(1)
    expect(tracker.started.value).toBe(false)
    expect(tracker.activeCombatantId.value).toBeNull()
  })

  it('loads state from localStorage', async () => {
    const saved = {
      combatants: [{
        id: 'test-1', type: 'PC', initiative: 15, name: 'Hero',
        currentHp: 20, maxHp: 25, ac: 16, conditions: '', status: 'active',
      }],
      activeCombatantId: 'test-1',
      round: 3,
      started: true,
    }
    mockStorage.setItem('soloforge-combat-tracker', JSON.stringify(saved))
    const { useCombatTracker } = await import('../useCombatTracker')
    const tracker = useCombatTracker()
    expect(tracker.combatants.value).toHaveLength(1)
    expect(tracker.combatants.value[0]!.name).toBe('Hero')
    expect(tracker.round.value).toBe(3)
    expect(tracker.started.value).toBe(true)
  })

  it('handles corrupted localStorage gracefully', async () => {
    mockStorage.setItem('soloforge-combat-tracker', '{invalid json')
    const { useCombatTracker } = await import('../useCombatTracker')
    const tracker = useCombatTracker()
    expect(tracker.combatants.value).toEqual([])
    expect(tracker.round.value).toBe(1)
  })

  // --- Add / Remove ---

  it('addCombatant creates with correct defaults', async () => {
    const { useCombatTracker } = await import('../useCombatTracker')
    const tracker = useCombatTracker()
    const c = tracker.addCombatant()
    expect(c.id).toBeTruthy()
    expect(c.type).toBe('NPC')
    expect(c.initiative).toBeNull()
    expect(c.name).toBe('')
    expect(c.currentHp).toBe(0)
    expect(c.maxHp).toBe(0)
    expect(c.ac).toBeNull()
    expect(c.conditions).toBe('')
    expect(c.status).toBe('active')
    expect(tracker.combatants.value).toHaveLength(1)
  })

  it('addCombatant generates unique ids', async () => {
    const { useCombatTracker } = await import('../useCombatTracker')
    const tracker = useCombatTracker()
    const c1 = tracker.addCombatant()
    const c2 = tracker.addCombatant()
    expect(c1.id).not.toBe(c2.id)
  })

  it('removeCombatant splices correctly', async () => {
    const { useCombatTracker } = await import('../useCombatTracker')
    const tracker = useCombatTracker()
    const c1 = tracker.addCombatant()
    const c2 = tracker.addCombatant()
    tracker.removeCombatant(c1.id)
    expect(tracker.combatants.value).toHaveLength(1)
    expect(tracker.combatants.value[0]!.id).toBe(c2.id)
  })

  it('removing active combatant advances to next living', async () => {
    const { useCombatTracker } = await import('../useCombatTracker')
    const tracker = useCombatTracker()
    const c1 = tracker.addCombatant()
    tracker.updateCombatant(c1.id, { currentHp: 10, maxHp: 10 })
    const c2 = tracker.addCombatant()
    tracker.updateCombatant(c2.id, { currentHp: 10, maxHp: 10 })
    tracker.nextTurn() // start combat, c1 active
    expect(tracker.activeCombatantId.value).toBe(c1.id)
    tracker.removeCombatant(c1.id)
    expect(tracker.activeCombatantId.value).toBe(c2.id)
  })

  it('removing active combatant advances forward, not to first in list', async () => {
    const { useCombatTracker } = await import('../useCombatTracker')
    const tracker = useCombatTracker()
    // Create 3 combatants: A(0), B(1), C(2)
    const a = tracker.addCombatant()
    tracker.updateCombatant(a.id, { currentHp: 10, maxHp: 10, name: 'A' })
    const b = tracker.addCombatant()
    tracker.updateCombatant(b.id, { currentHp: 10, maxHp: 10, name: 'B' })
    const c = tracker.addCombatant()
    tracker.updateCombatant(c.id, { currentHp: 10, maxHp: 10, name: 'C' })

    tracker.nextTurn() // start: A active
    tracker.nextTurn() // B active
    expect(tracker.activeCombatantId.value).toBe(b.id)

    // Remove B (active, at index 1) — should advance to C, not jump back to A
    tracker.removeCombatant(b.id)
    expect(tracker.activeCombatantId.value).toBe(c.id)
  })

  it('removing last active combatant stops combat', async () => {
    const { useCombatTracker } = await import('../useCombatTracker')
    const tracker = useCombatTracker()
    const a = tracker.addCombatant()
    tracker.updateCombatant(a.id, { currentHp: 10, maxHp: 10 })
    tracker.nextTurn() // start: A active
    tracker.removeCombatant(a.id)
    expect(tracker.activeCombatantId.value).toBeNull()
    expect(tracker.started.value).toBe(false)
  })

  // --- Initiative ---

  it('rollInitiative produces value between 1 and 20', async () => {
    const { useCombatTracker } = await import('../useCombatTracker')
    const tracker = useCombatTracker()
    const c = tracker.addCombatant()
    for (let i = 0; i < 50; i++) {
      tracker.rollInitiative(c.id)
      expect(tracker.combatants.value[0]!.initiative).toBeGreaterThanOrEqual(1)
      expect(tracker.combatants.value[0]!.initiative).toBeLessThanOrEqual(20)
    }
  })

  it('rollAllInitiative updates every combatant', async () => {
    const { useCombatTracker } = await import('../useCombatTracker')
    const tracker = useCombatTracker()
    tracker.addCombatant()
    tracker.addCombatant()
    tracker.addCombatant()
    tracker.rollAllInitiative()
    for (const c of tracker.combatants.value) {
      expect(c.initiative).toBeGreaterThanOrEqual(1)
      expect(c.initiative).toBeLessThanOrEqual(20)
    }
  })

  it('sortByInitiative orders descending with nulls last', async () => {
    const { useCombatTracker } = await import('../useCombatTracker')
    const tracker = useCombatTracker()
    const c1 = tracker.addCombatant()
    const c2 = tracker.addCombatant()
    const c3 = tracker.addCombatant()
    tracker.updateCombatant(c1.id, { initiative: 5 })
    tracker.updateCombatant(c2.id, { initiative: 18 })
    // c3 initiative stays null
    tracker.sortByInitiative()
    expect(tracker.combatants.value[0]!.id).toBe(c2.id)
    expect(tracker.combatants.value[1]!.id).toBe(c1.id)
    expect(tracker.combatants.value[2]!.id).toBe(c3.id)
  })

  // --- HP ---

  it('adjustHp applies positive and negative delta', async () => {
    const { useCombatTracker } = await import('../useCombatTracker')
    const tracker = useCombatTracker()
    const c = tracker.addCombatant()
    tracker.updateCombatant(c.id, { currentHp: 20, maxHp: 20 })
    tracker.adjustHp(c.id, -5)
    expect(tracker.combatants.value[0]!.currentHp).toBe(15)
    tracker.adjustHp(c.id, 3)
    expect(tracker.combatants.value[0]!.currentHp).toBe(18)
  })

  it('HP can go negative', async () => {
    const { useCombatTracker } = await import('../useCombatTracker')
    const tracker = useCombatTracker()
    const c = tracker.addCombatant()
    tracker.updateCombatant(c.id, { currentHp: 5, maxHp: 20 })
    tracker.adjustHp(c.id, -10)
    expect(tracker.combatants.value[0]!.currentHp).toBe(-5)
  })

  it('auto-sets dead when HP reaches 0', async () => {
    const { useCombatTracker } = await import('../useCombatTracker')
    const tracker = useCombatTracker()
    const c = tracker.addCombatant()
    tracker.updateCombatant(c.id, { currentHp: 5, maxHp: 20 })
    tracker.adjustHp(c.id, -5)
    expect(tracker.combatants.value[0]!.status).toBe('dead')
  })

  it('auto-sets dead when HP goes below 0', async () => {
    const { useCombatTracker } = await import('../useCombatTracker')
    const tracker = useCombatTracker()
    const c = tracker.addCombatant()
    tracker.updateCombatant(c.id, { currentHp: 5, maxHp: 20 })
    tracker.adjustHp(c.id, -10)
    expect(tracker.combatants.value[0]!.status).toBe('dead')
  })

  it('auto-sets dead when currentHp is set to 0 via updateCombatant', async () => {
    const { useCombatTracker } = await import('../useCombatTracker')
    const tracker = useCombatTracker()
    const c = tracker.addCombatant()
    tracker.updateCombatant(c.id, { currentHp: 10, maxHp: 20 })
    tracker.updateCombatant(c.id, { currentHp: 0 })
    expect(tracker.combatants.value[0]!.status).toBe('dead')
  })

  it('manual status toggle does not change HP', async () => {
    const { useCombatTracker } = await import('../useCombatTracker')
    const tracker = useCombatTracker()
    const c = tracker.addCombatant()
    tracker.updateCombatant(c.id, { currentHp: 15, maxHp: 20 })
    tracker.updateCombatant(c.id, { status: 'dead' })
    expect(tracker.combatants.value[0]!.currentHp).toBe(15)
    tracker.updateCombatant(c.id, { status: 'active' })
    expect(tracker.combatants.value[0]!.currentHp).toBe(15)
  })

  // --- Turn Tracking ---

  it('nextTurn starts combat and activates first living combatant', async () => {
    const { useCombatTracker } = await import('../useCombatTracker')
    const tracker = useCombatTracker()
    const c1 = tracker.addCombatant()
    tracker.updateCombatant(c1.id, { currentHp: 10, maxHp: 10 })
    tracker.addCombatant()
    expect(tracker.started.value).toBe(false)
    tracker.nextTurn()
    expect(tracker.started.value).toBe(true)
    expect(tracker.activeCombatantId.value).toBe(c1.id)
  })

  it('nextTurn skips dead combatants', async () => {
    const { useCombatTracker } = await import('../useCombatTracker')
    const tracker = useCombatTracker()
    const c1 = tracker.addCombatant()
    tracker.updateCombatant(c1.id, { currentHp: 10, maxHp: 10, name: 'A' })
    const c2 = tracker.addCombatant()
    tracker.updateCombatant(c2.id, { currentHp: 10, maxHp: 10, name: 'B', status: 'dead' })
    const c3 = tracker.addCombatant()
    tracker.updateCombatant(c3.id, { currentHp: 10, maxHp: 10, name: 'C' })

    tracker.nextTurn() // start: c1 active
    expect(tracker.activeCombatantId.value).toBe(c1.id)
    tracker.nextTurn() // skip c2 (dead), go to c3
    expect(tracker.activeCombatantId.value).toBe(c3.id)
  })

  it('nextTurn wrapping increments round', async () => {
    const { useCombatTracker } = await import('../useCombatTracker')
    const tracker = useCombatTracker()
    const c1 = tracker.addCombatant()
    tracker.updateCombatant(c1.id, { currentHp: 10, maxHp: 10 })
    const c2 = tracker.addCombatant()
    tracker.updateCombatant(c2.id, { currentHp: 10, maxHp: 10 })

    tracker.nextTurn() // start: c1
    expect(tracker.round.value).toBe(1)
    tracker.nextTurn() // c2
    expect(tracker.round.value).toBe(1)
    tracker.nextTurn() // wrap to c1, round 2
    expect(tracker.round.value).toBe(2)
    expect(tracker.activeCombatantId.value).toBe(c1.id)
  })

  it('prevTurn goes backward', async () => {
    const { useCombatTracker } = await import('../useCombatTracker')
    const tracker = useCombatTracker()
    const c1 = tracker.addCombatant()
    tracker.updateCombatant(c1.id, { currentHp: 10, maxHp: 10 })
    const c2 = tracker.addCombatant()
    tracker.updateCombatant(c2.id, { currentHp: 10, maxHp: 10 })
    const c3 = tracker.addCombatant()
    tracker.updateCombatant(c3.id, { currentHp: 10, maxHp: 10 })

    tracker.nextTurn() // start: c1
    tracker.nextTurn() // c2
    tracker.nextTurn() // c3
    tracker.prevTurn() // back to c2
    expect(tracker.activeCombatantId.value).toBe(c2.id)
  })

  it('prevTurn at start does nothing', async () => {
    const { useCombatTracker } = await import('../useCombatTracker')
    const tracker = useCombatTracker()
    const c1 = tracker.addCombatant()
    tracker.updateCombatant(c1.id, { currentHp: 10, maxHp: 10 })
    const c2 = tracker.addCombatant()
    tracker.updateCombatant(c2.id, { currentHp: 10, maxHp: 10 })

    tracker.nextTurn() // start: c1
    tracker.prevTurn() // should do nothing — already at first
    expect(tracker.activeCombatantId.value).toBe(c1.id)
    expect(tracker.round.value).toBe(1)
  })

  it('setRound overrides round number with minimum of 1', async () => {
    const { useCombatTracker } = await import('../useCombatTracker')
    const tracker = useCombatTracker()
    tracker.setRound(5)
    expect(tracker.round.value).toBe(5)
    tracker.setRound(0)
    expect(tracker.round.value).toBe(1)
    tracker.setRound(-3)
    expect(tracker.round.value).toBe(1)
  })

  // --- Reorder ---

  it('reorder moves item from index A to B', async () => {
    const { useCombatTracker } = await import('../useCombatTracker')
    const tracker = useCombatTracker()
    const c1 = tracker.addCombatant()
    tracker.updateCombatant(c1.id, { name: 'A' })
    const c2 = tracker.addCombatant()
    tracker.updateCombatant(c2.id, { name: 'B' })
    const c3 = tracker.addCombatant()
    tracker.updateCombatant(c3.id, { name: 'C' })

    tracker.reorder(2, 0)
    expect(tracker.combatants.value.map(c => c.name)).toEqual(['C', 'A', 'B'])
  })

  it('reorder ignores out-of-bounds', async () => {
    const { useCombatTracker } = await import('../useCombatTracker')
    const tracker = useCombatTracker()
    tracker.addCombatant()
    tracker.reorder(-1, 0)
    tracker.reorder(0, 5)
    expect(tracker.combatants.value).toHaveLength(1)
  })

  // --- Clear ---

  it('clearAll resets to defaults', async () => {
    const { useCombatTracker } = await import('../useCombatTracker')
    const tracker = useCombatTracker()
    tracker.addCombatant()
    tracker.addCombatant()
    tracker.nextTurn()
    tracker.setRound(5)
    tracker.clearAll()
    expect(tracker.combatants.value).toEqual([])
    expect(tracker.round.value).toBe(1)
    expect(tracker.started.value).toBe(false)
    expect(tracker.activeCombatantId.value).toBeNull()
  })

  // --- Persistence ---

  it('state changes are written to localStorage', async () => {
    const { useCombatTracker } = await import('../useCombatTracker')
    const tracker = useCombatTracker()
    tracker.addCombatant()
    // Vue watch is synchronous in test environment after ref mutation
    await vi.dynamicImportSettled()
    const stored = mockStorage.getItem('soloforge-combat-tracker')
    expect(stored).toBeTruthy()
    const parsed = JSON.parse(stored!)
    expect(parsed.combatants).toHaveLength(1)
  })

  // --- Character Suggestions ---

  it('getCharacterSuggestions returns all characters for empty query', async () => {
    const { useCombatTracker } = await import('../useCombatTracker')
    const tracker = useCombatTracker()
    const suggestions = tracker.getCharacterSuggestions('')
    expect(suggestions).toEqual(['Gandalf', 'Aragorn', 'Gimli'])
  })

  it('getCharacterSuggestions filters by query', async () => {
    const { useCombatTracker } = await import('../useCombatTracker')
    const tracker = useCombatTracker()
    const suggestions = tracker.getCharacterSuggestions('gan')
    expect(suggestions).toEqual(['Gandalf'])
  })

  it('getCharacterSuggestions is case-insensitive', async () => {
    const { useCombatTracker } = await import('../useCombatTracker')
    const tracker = useCombatTracker()
    const suggestions = tracker.getCharacterSuggestions('GIM')
    expect(suggestions).toEqual(['Gimli'])
  })
})
