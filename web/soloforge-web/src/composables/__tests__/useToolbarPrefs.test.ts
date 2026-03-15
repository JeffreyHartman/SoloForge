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

describe('useToolbarPrefs', () => {
  let mockStorage: Storage

  beforeEach(() => {
    vi.resetModules()
    mockStorage = createMockStorage()
    vi.stubGlobal('localStorage', mockStorage)
  })

  it('starts with empty items by default', async () => {
    const { useToolbarPrefs } = await import('../useToolbarPrefs')
    const { prefs } = useToolbarPrefs()
    expect(prefs.items).toEqual([])
  })

  it('loads stored prefs from localStorage', async () => {
    mockStorage.setItem('soloforge-toolbar-prefs', JSON.stringify({
      items: [{ type: 'tool', toolId: 'fate-check' }],
    }))
    const { useToolbarPrefs } = await import('../useToolbarPrefs')
    const { prefs } = useToolbarPrefs()
    expect(prefs.items).toHaveLength(1)
    expect(prefs.items[0]).toEqual({ type: 'tool', toolId: 'fate-check' })
  })

  it('pinTool adds a tool', async () => {
    const { useToolbarPrefs } = await import('../useToolbarPrefs')
    const { prefs, pinTool } = useToolbarPrefs()
    pinTool('fate-check')
    expect(prefs.items).toHaveLength(1)
    expect(prefs.items[0]).toEqual({ type: 'tool', toolId: 'fate-check' })
  })

  it('pinTool does not duplicate', async () => {
    const { useToolbarPrefs } = await import('../useToolbarPrefs')
    const { prefs, pinTool } = useToolbarPrefs()
    pinTool('fate-check')
    pinTool('fate-check')
    expect(prefs.items).toHaveLength(1)
  })

  it('unpinTool removes a tool', async () => {
    const { useToolbarPrefs } = await import('../useToolbarPrefs')
    const { prefs, pinTool, unpinTool } = useToolbarPrefs()
    pinTool('fate-check')
    unpinTool('fate-check')
    expect(prefs.items).toHaveLength(0)
  })

  it('moveItem reorders items', async () => {
    const { useToolbarPrefs } = await import('../useToolbarPrefs')
    const { prefs, pinTool, moveItem } = useToolbarPrefs()
    pinTool('a')
    pinTool('b')
    pinTool('c')
    moveItem(0, 2)
    expect(prefs.items.map(i => i.type === 'tool' ? i.toolId : '')).toEqual(['b', 'c', 'a'])
  })

  it('moveItem ignores out-of-bounds', async () => {
    const { useToolbarPrefs } = await import('../useToolbarPrefs')
    const { prefs, pinTool, moveItem } = useToolbarPrefs()
    pinTool('a')
    moveItem(-1, 0)
    moveItem(0, 5)
    expect(prefs.items).toHaveLength(1)
  })

  it('addSeparator and removeItem work', async () => {
    const { useToolbarPrefs } = await import('../useToolbarPrefs')
    const { prefs, addSeparator, removeItem } = useToolbarPrefs()
    addSeparator('Divider')
    expect(prefs.items).toHaveLength(1)
    expect(prefs.items[0]).toEqual({ type: 'separator', label: 'Divider' })
    removeItem(0)
    expect(prefs.items).toHaveLength(0)
  })

  it('updateSeparatorLabel updates label on separator items', async () => {
    const { useToolbarPrefs } = await import('../useToolbarPrefs')
    const { prefs, addSeparator, updateSeparatorLabel } = useToolbarPrefs()
    addSeparator('Old')
    updateSeparatorLabel(0, 'New')
    expect(prefs.items[0]).toEqual({ type: 'separator', label: 'New' })
  })

  it('updateSeparatorLabel clears label when empty string', async () => {
    const { useToolbarPrefs } = await import('../useToolbarPrefs')
    const { prefs, addSeparator, updateSeparatorLabel } = useToolbarPrefs()
    addSeparator('Label')
    updateSeparatorLabel(0, '')
    expect(prefs.items[0]).toEqual({ type: 'separator', label: undefined })
  })
})
