import { ref } from 'vue'

const STORAGE_KEY = 'soloforge-theme'

const isDark = ref(true)

// Initialize from localStorage or default to dark
function initTheme() {
  let stored: string | null = null
  try {
    stored = localStorage.getItem(STORAGE_KEY)
  } catch (e) {
    console.warn('Failed to read theme from localStorage:', e)
  }
  if (stored !== null) {
    isDark.value = stored === 'dark'
  } else {
    isDark.value = true // Default to dark mode
  }
  applyTheme()
}

function applyTheme() {
  if (isDark.value) {
    document.documentElement.classList.add('dark')
  } else {
    document.documentElement.classList.remove('dark')
  }
}

function toggleTheme() {
  isDark.value = !isDark.value
  localStorage.setItem(STORAGE_KEY, isDark.value ? 'dark' : 'light')
  applyTheme()
}

function setTheme(dark: boolean) {
  isDark.value = dark
  localStorage.setItem(STORAGE_KEY, isDark.value ? 'dark' : 'light')
  applyTheme()
}

export function useTheme() {
  return {
    isDark,
    initTheme,
    toggleTheme,
    setTheme,
  }
}
