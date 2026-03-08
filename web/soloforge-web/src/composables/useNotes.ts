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
let saving = false
let pendingSave = false
let debounceTimer: ReturnType<typeof setTimeout> | null = null
const DEBOUNCE_MS = 3000
let initialized = false

// Cache of tab contents so switching tabs is instant
const tabContentCache = new Map<string, string>()
const tabSavedCache = new Map<string, string>()

/** Schedules a debounced save of the active note content. */
function scheduleSave() {
  if (debounceTimer) clearTimeout(debounceTimer)
  debounceTimer = setTimeout(() => void executeSave(), DEBOUNCE_MS)
}

/** Persists the active note content to the API, handling concurrent save requests. */
async function executeSave() {
  const path = activeNotePath.value
  if (!currentCampaignId.value || !path) return

  while (true) {
    if (activeNoteContent.value === lastSavedContent) {
      saveStatus.value = 'saved'
      return
    }
    if (saving) {
      pendingSave = true
      return
    }

    saving = true
    loading.saveNote = true
    saveStatus.value = 'saving'
    const contentAtStart = activeNoteContent.value
    const savePath = path

    try {
      await apiSend<{ saved: boolean }>('/api/notes', 'PUT', {
        path: savePath,
        content: contentAtStart,
      })
      lastSavedContent = contentAtStart
      tabSavedCache.set(savePath, contentAtStart)
    } catch {
      saveStatus.value = 'unsaved'
      saving = false
      loading.saveNote = false
      return
    }

    saving = false
    loading.saveNote = false

    if (!pendingSave && activeNoteContent.value === contentAtStart) {
      saveStatus.value = 'saved'
      return
    }
    pendingSave = false
  }
}

/** Immediately triggers a save if there are unsaved changes, cancelling any pending debounce timer. */
function flushSave() {
  if (debounceTimer) {
    clearTimeout(debounceTimer)
    debounceTimer = null
  }
  if (activeNotePath.value && activeNoteContent.value !== lastSavedContent && currentCampaignId.value) {
    void executeSave()
  }
}

/** Fires a synchronous keepalive save on page unload to prevent data loss. */
function onBeforeUnload() {
  const path = activeNotePath.value
  if (!path || activeNoteContent.value === lastSavedContent || !currentCampaignId.value) return
  fetch('/api/notes', {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ path, content: activeNoteContent.value }),
    keepalive: true,
  })
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
    // Save current note before switching
    flushSave()

    // Add to tabs if not already open
    if (!openTabs.value.includes(path)) {
      openTabs.value.push(path)
    }

    activeNotePath.value = path

    // Check cache first
    if (tabContentCache.has(path)) {
      activeNoteContent.value = tabContentCache.get(path)!
      lastSavedContent = tabSavedCache.get(path) ?? activeNoteContent.value
      saveStatus.value = activeNoteContent.value === lastSavedContent ? 'saved' : 'unsaved'
      return
    }

    // Load from API
    loading.note = true
    try {
      const result = await apiGet<{ path: string; content: string }>(`/api/notes?path=${encodeURIComponent(path)}`)
      activeNoteContent.value = result.content ?? ''
      lastSavedContent = activeNoteContent.value
      tabContentCache.set(path, activeNoteContent.value)
      tabSavedCache.set(path, lastSavedContent)
      saveStatus.value = 'saved'
    } finally {
      loading.note = false
    }
  }

  /** Closes a tab, flushing saves and switching to an adjacent tab if the closed tab was active. */
  function closeTab(path: string) {
    // If closing the active tab, save first
    if (path === activeNotePath.value) {
      flushSave()
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
        void openNote(openTabs.value[newIdx])
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
      closeTab(path)
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
          closeTab(tab)
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
    flushSave,
    reloadActiveNote,
    invalidateTabCache,
    resetState,
  }
}
