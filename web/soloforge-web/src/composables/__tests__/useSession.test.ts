import { describe, it, expect, vi, beforeEach } from 'vitest'

vi.mock('../useApi', () => ({
  apiGet: vi.fn(),
  apiSend: vi.fn(),
}))

import { apiSend } from '../useApi'
import { useSession } from '../useSession'

describe('useSession', () => {
  beforeEach(() => {
    vi.mocked(apiSend).mockReset()
    vi.mocked(apiSend).mockResolvedValue({
      session: { chaos: 5, engine: 'Mythic 2e', theme: 'Fantasy' },
      adventure: { characters: [], activeThreads: [], closedThreads: [] },
      historyCount: 0,
    })
  })

  it('clamps chaos to max 9', async () => {
    const { chaosDraft, engineDraft, themeDraft, updateSession } = useSession()
    chaosDraft.value = 15
    engineDraft.value = 'Mythic 2e'
    themeDraft.value = 'Fantasy'

    await updateSession()

    expect(apiSend).toHaveBeenCalledWith(
      '/api/session', 'PUT',
      expect.objectContaining({ chaos: 9 }),
    )
  })

  it('clamps chaos to min 1', async () => {
    const { chaosDraft, engineDraft, themeDraft, updateSession } = useSession()
    chaosDraft.value = -3
    engineDraft.value = 'Mythic 2e'
    themeDraft.value = 'Fantasy'

    await updateSession()

    expect(apiSend).toHaveBeenCalledWith(
      '/api/session', 'PUT',
      expect.objectContaining({ chaos: 1 }),
    )
  })

  it('falls back to 5 for NaN chaos', async () => {
    const { chaosDraft, engineDraft, themeDraft, updateSession } = useSession()
    chaosDraft.value = NaN as unknown as number
    engineDraft.value = 'Mythic 2e'
    themeDraft.value = 'Fantasy'

    await updateSession()

    expect(apiSend).toHaveBeenCalledWith(
      '/api/session', 'PUT',
      expect.objectContaining({ chaos: 5 }),
    )
  })

  it('trims engine and theme', async () => {
    const { chaosDraft, engineDraft, themeDraft, updateSession } = useSession()
    chaosDraft.value = 5
    engineDraft.value = '  Mythic 2e  '
    themeDraft.value = '  Fantasy  '

    await updateSession()

    expect(apiSend).toHaveBeenCalledWith(
      '/api/session', 'PUT',
      expect.objectContaining({ engine: 'Mythic 2e', theme: 'Fantasy' }),
    )
  })

  it('syncFromState copies values to draft refs', () => {
    const { chaosDraft, engineDraft, themeDraft, syncFromState } = useSession()
    syncFromState({ chaos: 7, engine: 'Custom', theme: 'Horror' })
    expect(chaosDraft.value).toBe(7)
    expect(engineDraft.value).toBe('Custom')
    expect(themeDraft.value).toBe('Horror')
  })
})
