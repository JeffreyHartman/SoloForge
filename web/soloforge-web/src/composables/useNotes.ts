import { ref, reactive, watch, computed } from 'vue'
import { apiGet, apiSend } from './useApi'
import type { NoteNode, NoteTreeResponse, NoteListResponse } from '../types'

// === Shared state ===

const tree = ref<NoteNode[]>([])
const sessionLogPath = ref<string>('Session Log.md')
const allPaths = ref<string[]>([])
const currentCampaignId = ref<string | null>(null)

// Active note
const activeNotePath = ref<string | null>(null)
const activeNoteContent = ref<string>('')
const saveStatus = ref<'saved' | 'saving' | 'unsaved'>('saved')

// Open tabs
const openTabs = ref<string[]>([])

// Loading states
const loading = reactive({
  tree: false,
  note: false,
  saveNote: false,
  create: false,
  delete: false,
  move: false,
})

// Sidebar visibility
const sidebarOpen = ref(true)

// === Auto-save internals ===
let lastSavedContent = ''
let debounceTimer: ReturnType<typeof setTimeout> | null = null
const DEBOUNCE_MS = 3000
let initialized = false
let loadAbortController: AbortController | null = null
let currentSavePromise: Promise<void> | null = null
let openNoteGeneration = 0

// Cache of tab contents so switching tabs is instant
const tabContentCache = new Map<string, string>()
const tabSavedCache = new Map<string, string>()

/** Schedules a debounced save, capturing the current path and content at call time. */
function scheduleSave() {
  if (debounceTimer) clearTimeout(debounceTimer)
  const path = activeNotePath.value
  const content = activeNoteContent.value
  if (!path) return
  debounceTimer = setTimeout(() => {
    currentSavePromise = executeSave(path, content)
  }, DEBOUNCE_MS)
}

/** Persists note content to the API for a specific path. Path and content are captured by the caller to prevent cross-tab contamination. */
async function executeSave(savePath: string, saveContent: string): Promise<void> {
  if (!currentCampaignId.value || !savePath) return
  if (saveContent === (tabSavedCache.get(savePath) ?? '')) {
    if (activeNotePath.value === savePath) saveStatus.value = 'saved'
    return
  }

  loading.saveNote = true
  if (activeNotePath.value === savePath) saveStatus.value = 'saving'

  try {
    await apiSend<{ saved: boolean }>('/api/notes', 'PUT', {
      path: savePath,
      content: saveContent,
    })
    tabSavedCache.set(savePath, saveContent)
    if (activeNotePath.value === savePath) {
      lastSavedContent = saveContent
      if (activeNoteContent.value === saveContent) {
        saveStatus.value = 'saved'
      } else {
        saveStatus.value = 'unsaved'
      }
    }
  } catch {
    if (activeNotePath.value === savePath) {
      saveStatus.value = 'unsaved'
    }
  } finally {
    loading.saveNote = false
  }
}

/** Immediately triggers a save if there are unsaved changes, cancelling any pending debounce timer. Returns a promise that resolves when the save completes. */
async function flushSave(): Promise<void> {
  if (debounceTimer) {
    clearTimeout(debounceTimer)
    debounceTimer = null
  }
  const path = activeNotePath.value
  const content = activeNoteContent.value
  if (path && content !== lastSavedContent && currentCampaignId.value) {
    currentSavePromise = executeSave(path, content)
    await currentSavePromise
  } else if (currentSavePromise) {
    await currentSavePromise
  }
}

/** Fires keepalive saves on page unload for all tabs with unsaved changes. */
function onBeforeUnload() {
  if (!currentCampaignId.value) return
  // Save active tab
  const activePath = activeNotePath.value
  if (activePath && activeNoteContent.value !== lastSavedContent) {
    fetch('/api/notes', {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ path: activePath, content: activeNoteContent.value }),
      keepalive: true,
    })
  }
  // Save other tabs with unsaved changes
  for (const [tabPath, content] of tabContentCache) {
    if (tabPath === activePath) continue
    const saved = tabSavedCache.get(tabPath)
    if (content !== saved) {
      fetch('/api/notes', {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ path: tabPath, content }),
        keepalive: true,
      })
    }
  }
}

// === Public API ===

export function useNotes() {
  if (!initialized) {
    initialized = true

    watch(activeNoteContent, (newVal) => {
      // Update cache
      if (activeNotePath.value) {
        tabContentCache.set(activeNotePath.value, newVal)
      }

      if (newVal === lastSavedContent) {
        saveStatus.value = 'saved'
        return
      }
      saveStatus.value = 'unsaved'
      scheduleSave()
    })

    window.addEventListener('beforeunload', onBeforeUnload)
  }

  /** Fetches the note tree and path list from the API for the given campaign. */
  async function refreshTree(campaignId: string | null) {
    if (!campaignId) {
      tree.value = []
      allPaths.value = []
      currentCampaignId.value = null
      return
    }

    currentCampaignId.value = campaignId
    loading.tree = true
    try {
      const [treeResp, listResp] = await Promise.all([
        apiGet<NoteTreeResponse>('/api/notes/tree'),
        apiGet<NoteListResponse>('/api/notes/list'),
      ])
      tree.value = treeResp.tree
      sessionLogPath.value = treeResp.sessionLogPath
      allPaths.value = listResp.paths
    } finally {
      loading.tree = false
    }
  }

  /** Opens a note in the editor, adding it to tabs and loading from cache or API. */
  async function openNote(path: string) {
    const generation = ++openNoteGeneration

    // Save current note before switching (awaited to ensure correct path is used)
    await flushSave()

    // If another openNote was called while we were saving, bail out
    if (generation !== openNoteGeneration) return

    // Abort any in-flight note load
    if (loadAbortController) {
      loadAbortController.abort()
      loadAbortController = null
    }

    // Add to tabs if not already open
    if (!openTabs.value.includes(path)) {
      openTabs.value.push(path)
    }

    activeNotePath.value = path

    // Check cache first
    if (tabContentCache.has(path)) {
      // Set lastSavedContent BEFORE activeNoteContent so the watcher sees correct state
      lastSavedContent = tabSavedCache.get(path) ?? tabContentCache.get(path)!
      activeNoteContent.value = tabContentCache.get(path)!
      saveStatus.value = activeNoteContent.value === lastSavedContent ? 'saved' : 'unsaved'
      return
    }

    // Load from API with abort support
    loading.note = true
    loadAbortController = new AbortController()
    const { signal } = loadAbortController

    try {
      const result = await apiGet<{ path: string; content: string }>(
        `/api/notes?path=${encodeURIComponent(path)}`,
        { signal },
      )

      // If another openNote was called during the fetch, just cache and bail
      if (generation !== openNoteGeneration) {
        tabContentCache.set(path, result.content ?? '')
        tabSavedCache.set(path, result.content ?? '')
        return
      }

      const content = result.content ?? ''
      lastSavedContent = content
      activeNoteContent.value = content
      tabContentCache.set(path, content)
      tabSavedCache.set(path, content)
      saveStatus.value = 'saved'
    } catch (e: unknown) {
      if (e instanceof DOMException && e.name === 'AbortError') return
      throw e
    } finally {
      loading.note = false
    }
  }

  /** Closes a tab, flushing saves and switching to an adjacent tab if the closed tab was active. */
  async function closeTab(path: string) {
    // If closing the active tab, save first
    if (path === activeNotePath.value) {
      await flushSave()
    }

    const idx = openTabs.value.indexOf(path)
    if (idx === -1) return

    openTabs.value.splice(idx, 1)
    tabContentCache.delete(path)
    tabSavedCache.delete(path)

    if (path === activeNotePath.value) {
      if (openTabs.value.length > 0) {
        // Switch to the nearest tab
        const newIdx = Math.min(idx, openTabs.value.length - 1)
        await openNote(openTabs.value[newIdx]!)
      } else {
        activeNotePath.value = null
        activeNoteContent.value = ''
        lastSavedContent = ''
        saveStatus.value = 'saved'
      }
    }
  }

  /** Creates a new note at the given path and opens it in the editor. */
  async function createNote(path: string, content?: string) {
    if (!currentCampaignId.value) return
    loading.create = true
    try {
      await apiSend('/api/notes', 'POST', { path, content: content ?? '' })
      await refreshTree(currentCampaignId.value)
      await openNote(path)
    } finally {
      loading.create = false
    }
  }

  /** Deletes a note, closing its tab and refreshing the tree. */
  async function deleteNote(path: string) {
    if (!currentCampaignId.value) return
    loading.delete = true
    try {
      await apiSend(`/api/notes?path=${encodeURIComponent(path)}`, 'DELETE')
      await closeTab(path)
      await refreshTree(currentCampaignId.value)
    } finally {
      loading.delete = false
    }
  }

  /** Creates a new folder in the vault and refreshes the tree. */
  async function createFolder(path: string) {
    if (!currentCampaignId.value) return
    loading.create = true
    try {
      await apiSend('/api/notes/folder', 'POST', { path })
      await refreshTree(currentCampaignId.value)
    } finally {
      loading.create = false
    }
  }

  /** Deletes a folder and all its contents, closing any open tabs within it. */
  async function deleteFolder(path: string) {
    if (!currentCampaignId.value) return
    loading.delete = true
    try {
      await apiSend(`/api/notes/folder?path=${encodeURIComponent(path)}`, 'DELETE')
      // Close any open tabs that were in this folder
      for (const tab of [...openTabs.value]) {
        if (tab === path || tab.startsWith(path + '/')) {
          await closeTab(tab)
        }
      }
      await refreshTree(currentCampaignId.value)
    } finally {
      loading.delete = false
    }
  }

  /** Moves or renames a note/folder, updating tab references and caches. */
  async function moveItem(oldPath: string, newPath: string) {
    if (!currentCampaignId.value) return
    loading.move = true
    try {
      await apiSend('/api/notes/move', 'POST', { oldPath, newPath })

      // Update tab references
      const tabIdx = openTabs.value.indexOf(oldPath)
      if (tabIdx !== -1) {
        openTabs.value[tabIdx] = newPath
        const cached = tabContentCache.get(oldPath)
        const saved = tabSavedCache.get(oldPath)
        if (cached !== undefined) {
          tabContentCache.set(newPath, cached)
          tabContentCache.delete(oldPath)
        }
        if (saved !== undefined) {
          tabSavedCache.set(newPath, saved)
          tabSavedCache.delete(oldPath)
        }
        if (activeNotePath.value === oldPath) {
          activeNotePath.value = newPath
        }
      }

      await refreshTree(currentCampaignId.value)
    } finally {
      loading.move = false
    }
  }

  /** Designates a note as the session log (roll results auto-append to it). */
  async function setSessionLog(path: string) {
    if (!currentCampaignId.value) return
    await apiSend('/api/notes/session-log', 'PUT', { path })
    sessionLogPath.value = path
    // Refresh tree so the badge updates everywhere
    await refreshTree(currentCampaignId.value)
  }

  /** Clears the cache for the active note and re-fetches its content from the API. */
  async function reloadActiveNote() {
    const path = activeNotePath.value
    if (!path || !currentCampaignId.value) return

    // Clear cache so openNote fetches fresh from API
    tabContentCache.delete(path)
    tabSavedCache.delete(path)
    await openNote(path)
  }

  /** Removes a note's content from the tab cache so the next open fetches fresh data. */
  function invalidateTabCache(path: string) {
    tabContentCache.delete(path)
    tabSavedCache.delete(path)
  }

  /** Resets all notes state (tree, tabs, caches) — used when switching campaigns. */
  function resetState() {
    tree.value = []
    allPaths.value = []
    activeNotePath.value = null
    activeNoteContent.value = ''
    lastSavedContent = ''
    openTabs.value = []
    tabContentCache.clear()
    tabSavedCache.clear()
    saveStatus.value = 'saved'
    sidebarOpen.value = true
  }

  const activeNoteFileName = computed(() => {
    if (!activeNotePath.value) return null
    const name = activeNotePath.value.split('/').pop() ?? ''
    return name.endsWith('.md') ? name.slice(0, -3) : name
  })

  /**
   * Resolves a wiki-link path against the known note paths.
   * If the path contains a `/` it's already qualified — return as-is.
   * Otherwise, search allPaths for a matching filename (case-insensitive).
   * Prefers a root-level exact match over a subdirectory match.
   */
  function resolveNotePath(path: string): string {
    if (path.includes('/')) return path
    const lower = path.toLowerCase()

    // Prefer root-level exact match (path === filename, no directory prefix)
    const rootMatch = allPaths.value.find(p => p.toLowerCase() === lower)
    if (rootMatch) return rootMatch

    // Fall back to any file with matching filename
    const match = allPaths.value.find(p => (p.split('/').pop() ?? '').toLowerCase() === lower)
    return match ?? path
  }

  return {
    // State
    tree,
    sessionLogPath,
    allPaths,
    activeNotePath,
    activeNoteContent,
    activeNoteFileName,
    saveStatus,
    openTabs,
    loading,
    sidebarOpen,

    // Actions
    refreshTree,
    openNote,
    closeTab,
    createNote,
    deleteNote,
    createFolder,
    deleteFolder,
    moveItem,
    setSessionLog,
    resolveNotePath,
    flushSave,
    reloadActiveNote,
    invalidateTabCache,
    resetState,
  }
}
