import { reactive, watch } from 'vue'

export type ToolbarItem =
  | { type: 'tool'; toolId: string }
  | { type: 'separator'; label?: string }

export interface ToolbarPrefs {
  items: ToolbarItem[]
}

const STORAGE_KEY = 'soloforge-toolbar-prefs'

const DEFAULTS: ToolbarPrefs = {
  items: [],
}

function load(): ToolbarPrefs {
  try {
    const stored = localStorage.getItem(STORAGE_KEY)
    if (stored) return { ...DEFAULTS, ...JSON.parse(stored) }
  } catch { /* ignore corrupted storage */ }
  return { ...DEFAULTS }
}

const prefs = reactive<ToolbarPrefs>(load())

watch(prefs, (val) => {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(val))
}, { deep: true })

function pinTool(id: string) {
  if (isPinned(id)) return
  prefs.items.push({ type: 'tool', toolId: id })
}

function unpinTool(id: string) {
  const idx = prefs.items.findIndex(i => i.type === 'tool' && i.toolId === id)
  if (idx !== -1) prefs.items.splice(idx, 1)
}

function isPinned(id: string): boolean {
  return prefs.items.some(i => i.type === 'tool' && i.toolId === id)
}

function moveItem(from: number, to: number) {
  if (from < 0 || from >= prefs.items.length || to < 0 || to >= prefs.items.length) return
  const [item] = prefs.items.splice(from, 1) as [ToolbarItem]
  prefs.items.splice(to, 0, item)
}

function addSeparator(label?: string) {
  prefs.items.push({ type: 'separator', label })
}

function removeItem(index: number) {
  if (index >= 0 && index < prefs.items.length) {
    prefs.items.splice(index, 1)
  }
}

function updateSeparatorLabel(index: number, label: string) {
  const item = prefs.items[index]
  if (item && item.type === 'separator') {
    item.label = label || undefined
  }
}

export function useToolbarPrefs() {
  return {
    prefs,
    pinTool,
    unpinTool,
    isPinned,
    moveItem,
    addSeparator,
    removeItem,
    updateSeparatorLabel,
  }
}
