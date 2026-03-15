import { describe, it, expect, vi, beforeEach } from 'vitest'

describe('useNameGenerator', () => {
  beforeEach(() => {
    vi.resetModules()
  })

  it('generates the requested number of names', async () => {
    const { useNameGenerator } = await import('../useNameGenerator')
    const { count, results, generate } = useNameGenerator()
    count.value = 3
    generate()
    expect(results.value).toHaveLength(3)
  })

  it('clamps count to pool size', async () => {
    const { useNameGenerator } = await import('../useNameGenerator')
    const { count, results, generate, selectedStyle } = useNameGenerator()
    const poolSize = selectedStyle.value.names.length
    count.value = poolSize + 100
    generate()
    expect(results.value).toHaveLength(poolSize)
  })

  it('clamps count to minimum 1', async () => {
    const { useNameGenerator } = await import('../useNameGenerator')
    const { count, results, generate } = useNameGenerator()
    count.value = -5
    generate()
    expect(results.value).toHaveLength(1)
  })

  it('produces unique names (no duplicates)', async () => {
    const { useNameGenerator } = await import('../useNameGenerator')
    const { count, results, generate } = useNameGenerator()
    count.value = 10
    generate()
    const unique = new Set(results.value)
    expect(unique.size).toBe(results.value.length)
  })

  it('clear empties results', async () => {
    const { useNameGenerator } = await import('../useNameGenerator')
    const { count, results, generate, clear } = useNameGenerator()
    count.value = 3
    generate()
    expect(results.value.length).toBeGreaterThan(0)
    clear()
    expect(results.value).toEqual([])
  })
})
