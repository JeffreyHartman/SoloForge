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

describe('useTheme', () => {
  let mockStorage: Storage

  beforeEach(() => {
    vi.resetModules()
    mockStorage = createMockStorage()
    vi.stubGlobal('localStorage', mockStorage)
  })

  it('applies default theme when no stored value', async () => {
    const { useTheme } = await import('../useTheme')
    const { initTheme, currentThemeId } = useTheme()
    initTheme()
    expect(currentThemeId.value).toBe('obsidian')
  })

  it('migrates "dark" to "obsidian"', async () => {
    mockStorage.setItem('soloforge-theme', 'dark')
    const { useTheme } = await import('../useTheme')
    const { initTheme, currentThemeId } = useTheme()
    initTheme()
    expect(currentThemeId.value).toBe('obsidian')
    expect(mockStorage.getItem('soloforge-theme')).toBe('obsidian')
  })

  it('migrates "light" to "parchment"', async () => {
    mockStorage.setItem('soloforge-theme', 'light')
    const { useTheme } = await import('../useTheme')
    const { initTheme, currentThemeId } = useTheme()
    initTheme()
    expect(currentThemeId.value).toBe('parchment')
    expect(mockStorage.getItem('soloforge-theme')).toBe('parchment')
  })

  it('falls back to default for invalid stored theme', async () => {
    mockStorage.setItem('soloforge-theme', 'nonexistent')
    const { useTheme } = await import('../useTheme')
    const { initTheme, currentThemeId } = useTheme()
    initTheme()
    expect(currentThemeId.value).toBe('obsidian')
  })

  it('restores a valid stored theme', async () => {
    mockStorage.setItem('soloforge-theme', 'sci-fi')
    const { useTheme } = await import('../useTheme')
    const { initTheme, currentThemeId } = useTheme()
    initTheme()
    expect(currentThemeId.value).toBe('sci-fi')
  })

  it('setTheme rejects unknown themes', async () => {
    const { useTheme } = await import('../useTheme')
    const { initTheme, setTheme, currentThemeId } = useTheme()
    initTheme()
    setTheme('invalid-id')
    expect(currentThemeId.value).toBe('obsidian')
  })

  it('isDark returns true for dark themes', async () => {
    const { useTheme } = await import('../useTheme')
    const { initTheme, isDark } = useTheme()
    initTheme()
    expect(isDark.value).toBe(true)
  })

  it('isDark returns false for light themes', async () => {
    mockStorage.setItem('soloforge-theme', 'parchment')
    const { useTheme } = await import('../useTheme')
    const { initTheme, isDark } = useTheme()
    initTheme()
    expect(isDark.value).toBe(false)
  })

  it('toggleTheme switches dark to light', async () => {
    const { useTheme } = await import('../useTheme')
    const { initTheme, toggleTheme, currentThemeId } = useTheme()
    initTheme()
    toggleTheme()
    expect(currentThemeId.value).toBe('parchment')
  })
})
