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

describe('useToolNav', () => {
  let mockStorage: Storage

  beforeEach(() => {
    vi.resetModules()
    mockStorage = createMockStorage()
    vi.stubGlobal('localStorage', mockStorage)
  })

  it('defaults sidebar to open', async () => {
    const { useToolNav } = await import('../useToolNav')
    const { sidebarOpen } = useToolNav()
    expect(sidebarOpen.value).toBe(true)
  })

  it('restores sidebar state from localStorage', async () => {
    mockStorage.setItem('soloforge-tools-sidebar-open', 'false')
    const { useToolNav } = await import('../useToolNav')
    const { sidebarOpen } = useToolNav()
    expect(sidebarOpen.value).toBe(false)
  })

  it('restores collapsed groups from localStorage', async () => {
    mockStorage.setItem('soloforge-tools-collapsed-groups', JSON.stringify(['group1', 'group2']))
    const { useToolNav } = await import('../useToolNav')
    const { isGroupCollapsed } = useToolNav()
    expect(isGroupCollapsed('group1')).toBe(true)
    expect(isGroupCollapsed('group2')).toBe(true)
    expect(isGroupCollapsed('group3')).toBe(false)
  })

  it('handles corrupted JSON in collapsed groups gracefully', async () => {
    mockStorage.setItem('soloforge-tools-collapsed-groups', 'not-json')
    const { useToolNav } = await import('../useToolNav')
    const { collapsedGroups } = useToolNav()
    expect(collapsedGroups.value.size).toBe(0)
  })

  it('toggleGroup adds and removes groups', async () => {
    const { useToolNav } = await import('../useToolNav')
    const { toggleGroup, isGroupCollapsed } = useToolNav()
    expect(isGroupCollapsed('test')).toBe(false)
    toggleGroup('test')
    expect(isGroupCollapsed('test')).toBe(true)
    toggleGroup('test')
    expect(isGroupCollapsed('test')).toBe(false)
  })

  it('selectPage updates active page', async () => {
    const { useToolNav } = await import('../useToolNav')
    const { activePage, selectPage } = useToolNav()
    selectPage('new-page')
    expect(activePage.value).toBe('new-page')
  })

  it('toggleSidebar flips the value', async () => {
    const { useToolNav } = await import('../useToolNav')
    const { sidebarOpen, toggleSidebar } = useToolNav()
    const initial = sidebarOpen.value
    toggleSidebar()
    expect(sidebarOpen.value).toBe(!initial)
  })
})
