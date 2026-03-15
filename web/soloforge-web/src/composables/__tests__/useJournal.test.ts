import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'

vi.mock('../useApi', () => ({
  apiGet: vi.fn(),
  apiSend: vi.fn(),
}))

import { apiGet, apiSend } from '../useApi'

describe('useJournal', () => {
  beforeEach(() => {
    vi.useFakeTimers()
    vi.resetModules()
    vi.mocked(apiGet).mockReset()
    vi.mocked(apiSend).mockReset()
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('refreshJournal loads content and marks as saved', async () => {
    vi.mocked(apiGet).mockResolvedValue({ campaignId: 'c1', content: 'Hello journal' })
    const { useJournal } = await import('../useJournal')
    const { journal, saveStatus, refreshJournal } = useJournal()
    await refreshJournal('c1')
    expect(journal.value).toBe('Hello journal')
    expect(saveStatus.value).toBe('saved')
  })

  it('refreshJournal with null clears journal', async () => {
    const { useJournal } = await import('../useJournal')
    const { journal, saveStatus, refreshJournal } = useJournal()
    await refreshJournal(null)
    expect(journal.value).toBe('')
    expect(saveStatus.value).toBe('saved')
  })

  it('auto-save triggers after debounce period', async () => {
    vi.mocked(apiGet).mockResolvedValue({ campaignId: 'c1', content: '' })
    vi.mocked(apiSend).mockResolvedValue({ saved: true })

    const { useJournal } = await import('../useJournal')
    const { journal, saveStatus, refreshJournal } = useJournal()

    await refreshJournal('c1')

    // Modify journal content
    journal.value = 'Updated content'

    // Watcher needs a microtask tick to fire
    await vi.advanceTimersByTimeAsync(0)
    expect(saveStatus.value).toBe('unsaved')

    // Advance past debounce period (3000ms)
    await vi.advanceTimersByTimeAsync(3000)

    expect(apiSend).toHaveBeenCalledWith('/api/journal', 'PUT', {
      campaignId: 'c1',
      content: 'Updated content',
    })
  })

  it('flushSave triggers immediate save', async () => {
    vi.mocked(apiGet).mockResolvedValue({ campaignId: 'c1', content: '' })
    vi.mocked(apiSend).mockResolvedValue({ saved: true })

    const { useJournal } = await import('../useJournal')
    const { journal, refreshJournal, flushSave } = useJournal()

    await refreshJournal('c1')
    journal.value = 'New content'
    await vi.advanceTimersByTimeAsync(0) // trigger watcher

    flushSave()
    await vi.advanceTimersByTimeAsync(0)

    expect(apiSend).toHaveBeenCalledWith('/api/journal', 'PUT', expect.objectContaining({
      content: 'New content',
    }))
  })

  it('save failure sets status to unsaved', async () => {
    vi.mocked(apiGet).mockResolvedValue({ campaignId: 'c1', content: '' })
    vi.mocked(apiSend).mockRejectedValue(new Error('Network error'))

    const { useJournal } = await import('../useJournal')
    const { journal, saveStatus, refreshJournal } = useJournal()

    await refreshJournal('c1')
    journal.value = 'Some changes'
    await vi.advanceTimersByTimeAsync(0)
    await vi.advanceTimersByTimeAsync(3000)

    expect(saveStatus.value).toBe('unsaved')
  })
})
