import { ref, computed } from 'vue'

export interface WebTheme {
  id: string
  name: string
  genre: string
  isDark: boolean
  /** Preview colors for the theme picker swatches */
  preview: { bg: string; card: string; text: string; accent: string }
}

const STORAGE_KEY = 'soloforge-theme'
const DEFAULT_THEME = 'obsidian'

export const WEB_THEMES: WebTheme[] = [
  {
    id: 'parchment',
    name: 'Parchment',
    genre: 'Classic Light',
    isDark: false,
    preview: { bg: '#f5edd8', card: '#fef9eb', text: '#2c1a10', accent: '#5c3d2e' },
  },
  {
    id: 'obsidian',
    name: 'Obsidian',
    genre: 'Modern Dark',
    isDark: true,
    preview: { bg: '#0f172a', card: '#1e293b', text: '#f1f5f9', accent: '#f1f5f9' },
  },
  {
    id: 'fantasy',
    name: 'Fantasy',
    genre: 'Candlelit Tavern',
    isDark: true,
    preview: { bg: '#18130e', card: '#282018', text: '#e8d5b0', accent: '#c9a84c' },
  },
  {
    id: 'sci-fi',
    name: 'Sci-Fi',
    genre: 'Holographic HUD',
    isDark: true,
    preview: { bg: '#080c18', card: '#121d32', text: '#c8e4f0', accent: '#00d4ff' },
  },
  {
    id: 'eldritch',
    name: 'Eldritch',
    genre: 'Cosmic Horror',
    isDark: true,
    preview: { bg: '#0c0814', card: '#1a1438', text: '#d0c4e8', accent: '#7cd42a' },
  },
  {
    id: 'dragon-fire',
    name: 'Dragon Fire',
    genre: 'Volcanic Fury',
    isDark: true,
    preview: { bg: '#160808', card: '#281412', text: '#f0d0b0', accent: '#ff6b2b' },
  },
  {
    id: 'fey-wild',
    name: 'Fey Wild',
    genre: 'Enchanted Forest',
    isDark: false,
    preview: { bg: '#ebf2e6', card: '#f6faf4', text: '#1a3028', accent: '#7b3fa0' },
  },
  {
    id: 'noir',
    name: 'Noir',
    genre: 'Shadow Detective',
    isDark: true,
    preview: { bg: '#060606', card: '#141414', text: '#e0e0e0', accent: '#dc2626' },
  },
  {
    id: 'steampunk',
    name: 'Steampunk',
    genre: 'Brass & Clockwork',
    isDark: true,
    preview: { bg: '#12100a', card: '#221c14', text: '#d4b896', accent: '#cd7f32' },
  },
  {
    id: 'amber-terminal',
    name: 'Amber Terminal',
    genre: 'Retro CRT',
    isDark: true,
    preview: { bg: '#000000', card: '#0e0e00', text: '#ffb000', accent: '#ffb000' },
  },
  {
    id: 'green-terminal',
    name: 'Green Terminal',
    genre: 'Mainframe',
    isDark: true,
    preview: { bg: '#000000', card: '#000e00', text: '#33ff33', accent: '#33ff33' },
  },
]

const currentThemeId = ref(DEFAULT_THEME)

const currentTheme = computed(() =>
  WEB_THEMES.find(t => t.id === currentThemeId.value) ?? WEB_THEMES[1]
)

const isDark = computed(() => currentTheme.value!.isDark)

function applyTheme(id: string) {
  const theme = WEB_THEMES.find(t => t.id === id)
  if (!theme) return
  currentThemeId.value = id
  document.documentElement.setAttribute('data-theme', id)
  // Maintain .dark class for any Tailwind dark: utilities still in use
  if (theme.isDark) {
    document.documentElement.classList.add('dark')
  } else {
    document.documentElement.classList.remove('dark')
  }
}

function initTheme() {
  let stored: string | null = null
  try {
    stored = localStorage.getItem(STORAGE_KEY)
  } catch {
    // localStorage unavailable
  }

  // Migrate old light/dark values and persist the migrated key
  if (stored === 'dark' || stored === 'light') {
    stored = stored === 'dark' ? 'obsidian' : 'parchment'
    try { localStorage.setItem(STORAGE_KEY, stored) } catch { /* noop */ }
  }

  const themeId = stored && WEB_THEMES.some(t => t.id === stored) ? stored : DEFAULT_THEME
  applyTheme(themeId)
}

function setTheme(id: string) {
  if (!WEB_THEMES.some(t => t.id === id)) return
  applyTheme(id)
  try {
    localStorage.setItem(STORAGE_KEY, id)
  } catch {
    // localStorage unavailable
  }
}

function toggleTheme() {
  setTheme(isDark.value ? 'parchment' : 'obsidian')
}

export function useTheme() {
  return {
    themes: WEB_THEMES,
    currentThemeId,
    currentTheme,
    isDark,
    initTheme,
    setTheme,
    toggleTheme,
  }
}
