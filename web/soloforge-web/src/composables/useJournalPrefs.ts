import { reactive, watch } from 'vue'

export interface JournalPrefs {
  mode: 'edit' | 'preview'
  split: boolean
  enhanced: boolean
  fontFamily: 'mono' | 'sans' | 'serif'
  fontSize: number
}

const STORAGE_KEY = 'soloforge-journal-prefs'

const DEFAULTS: JournalPrefs = {
  mode: 'edit',
  split: false,
  enhanced: true,
  fontFamily: 'mono',
  fontSize: 14,
}

function load(): JournalPrefs {
  try {
    const stored = localStorage.getItem(STORAGE_KEY)
    if (stored) return { ...DEFAULTS, ...JSON.parse(stored) }
  } catch { /* ignore corrupted storage */ }
  return { ...DEFAULTS }
}

const prefs = reactive<JournalPrefs>(load())

export function useJournalPrefs() {
  watch(prefs, (val) => {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(val))
  }, { deep: true })

  return { prefs }
}

export const FONT_FAMILIES: Record<string, string> = {
  mono: "ui-monospace, 'Cascadia Code', 'Fira Code', monospace",
  sans: "'Space Grotesk', ui-sans-serif, system-ui, sans-serif",
  serif: "Georgia, 'Times New Roman', ui-serif, serif",
}
