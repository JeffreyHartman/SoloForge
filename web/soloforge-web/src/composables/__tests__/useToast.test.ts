import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'

describe('useToast', () => {
  beforeEach(() => {
    vi.useFakeTimers()
    vi.resetModules()
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('adds a toast with a unique ID', async () => {
    const { useToast } = await import('../useToast')
    const { toasts, addToast } = useToast()
    addToast({ title: 'Saved', variant: 'success' })
    expect(toasts).toHaveLength(1)
    expect(toasts[0]!.id).toMatch(/^toast-/)
    expect(toasts[0]!.title).toBe('Saved')
  })

  it('generates incrementing IDs', async () => {
    const { useToast } = await import('../useToast')
    const { toasts, addToast } = useToast()
    addToast({ title: 'A', variant: 'info' })
    addToast({ title: 'B', variant: 'info' })
    expect(toasts[0]!.id).not.toBe(toasts[1]!.id)
  })

  it('auto-dismisses after 4000ms', async () => {
    const { useToast } = await import('../useToast')
    const { toasts, addToast } = useToast()
    addToast({ title: 'Saved', variant: 'success' })
    expect(toasts).toHaveLength(1)
    vi.advanceTimersByTime(4000)
    expect(toasts).toHaveLength(0)
  })

  it('manually dismisses a toast', async () => {
    const { useToast } = await import('../useToast')
    const { toasts, addToast, dismissToast } = useToast()
    addToast({ title: 'A', variant: 'info' })
    const id = toasts[0]!.id
    dismissToast(id)
    expect(toasts).toHaveLength(0)
  })

  it('dismissToast ignores unknown IDs', async () => {
    const { useToast } = await import('../useToast')
    const { toasts, addToast, dismissToast } = useToast()
    addToast({ title: 'A', variant: 'info' })
    dismissToast('nonexistent')
    expect(toasts).toHaveLength(1)
  })
})
