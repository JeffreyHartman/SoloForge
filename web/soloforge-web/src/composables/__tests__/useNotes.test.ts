import { describe, it, expect, vi, beforeEach } from 'vitest'

vi.mock('../useApi', () => ({
  apiGet: vi.fn(),
  apiSend: vi.fn(),
}))

import { apiGet } from '../useApi'

describe('useNotes', () => {
  beforeEach(() => {
    vi.resetModules()
    vi.mocked(apiGet).mockReset()
  })

  describe('resolveNotePath', () => {
    it('returns path as-is if it contains a slash', async () => {
      const { useNotes } = await import('../useNotes')
      const { resolveNotePath } = useNotes()
      expect(resolveNotePath('folder/note.md')).toBe('folder/note.md')
    })

    it('finds root-level exact match (case-insensitive)', async () => {
      const { useNotes } = await import('../useNotes')
      const { resolveNotePath, allPaths } = useNotes()
      allPaths.value = ['MyNote.md', 'subdir/MyNote.md']
      expect(resolveNotePath('mynote.md')).toBe('MyNote.md')
    })

    it('falls back to filename match in subdirectory', async () => {
      const { useNotes } = await import('../useNotes')
      const { resolveNotePath, allPaths } = useNotes()
      allPaths.value = ['subdir/Target.md']
      expect(resolveNotePath('Target.md')).toBe('subdir/Target.md')
    })

    it('returns original path when no match found', async () => {
      const { useNotes } = await import('../useNotes')
      const { resolveNotePath, allPaths } = useNotes()
      allPaths.value = ['other.md']
      expect(resolveNotePath('missing.md')).toBe('missing.md')
    })
  })

  describe('activeNoteFileName', () => {
    it('returns null when no active note', async () => {
      const { useNotes } = await import('../useNotes')
      const { activeNoteFileName } = useNotes()
      expect(activeNoteFileName.value).toBeNull()
    })

    it('strips .md extension from filename', async () => {
      const { useNotes } = await import('../useNotes')
      const { activeNotePath, activeNoteFileName } = useNotes()
      activeNotePath.value = 'folder/My Note.md'
      expect(activeNoteFileName.value).toBe('My Note')
    })

    it('handles path without .md extension', async () => {
      const { useNotes } = await import('../useNotes')
      const { activeNotePath, activeNoteFileName } = useNotes()
      activeNotePath.value = 'folder/readme'
      expect(activeNoteFileName.value).toBe('readme')
    })
  })

  describe('resetState', () => {
    it('clears all state', async () => {
      const { useNotes } = await import('../useNotes')
      const { tree, allPaths, activeNotePath, activeNoteContent, openTabs, saveStatus, sidebarOpen, resetState } = useNotes()

      // Set some state
      tree.value = [{ name: 'test', path: 'test.md', isFolder: false, children: [] }]
      allPaths.value = ['test.md']
      activeNotePath.value = 'test.md'
      activeNoteContent.value = 'content'
      openTabs.value = ['test.md']

      resetState()

      expect(tree.value).toEqual([])
      expect(allPaths.value).toEqual([])
      expect(activeNotePath.value).toBeNull()
      expect(activeNoteContent.value).toBe('')
      expect(openTabs.value).toEqual([])
      expect(saveStatus.value).toBe('saved')
      expect(sidebarOpen.value).toBe(true)
    })
  })

  describe('refreshTree', () => {
    it('clears state when called with null', async () => {
      const { useNotes } = await import('../useNotes')
      const { tree, allPaths, refreshTree } = useNotes()
      tree.value = [{ name: 'x', path: 'x.md', isFolder: false, children: [] }]
      allPaths.value = ['x.md']

      await refreshTree(null)

      expect(tree.value).toEqual([])
      expect(allPaths.value).toEqual([])
    })

    it('loads tree and paths from API', async () => {
      vi.mocked(apiGet).mockImplementation(async (url: string) => {
        if (url.includes('tree')) return { campaignId: 'c1', sessionLogPath: 'Session Log.md', tree: [{ name: 'Note', path: 'Note.md', isFolder: false, children: [] }] }
        if (url.includes('list')) return { campaignId: 'c1', paths: ['Note.md'] }
        throw new Error('unexpected url')
      })

      const { useNotes } = await import('../useNotes')
      const { tree, allPaths, refreshTree } = useNotes()
      await refreshTree('c1')

      expect(tree.value).toHaveLength(1)
      expect(allPaths.value).toEqual(['Note.md'])
    })
  })
})
