import { ref, watch } from 'vue'
import { TOOL_GROUPS } from '../tools/registry'

const STORAGE_KEY_SIDEBAR = 'soloforge-tools-sidebar-open'
const STORAGE_KEY_COLLAPSED = 'soloforge-tools-collapsed-groups'
const STORAGE_KEY_PAGE = 'soloforge-tools-active-page'

function loadBoolean(key: string, fallback: boolean): boolean {
  const stored = localStorage.getItem(key)
  if (stored === null) return fallback
  return stored === 'true'
}

function loadStringSet(key: string): Set<string> {
  try {
    const stored = localStorage.getItem(key)
    if (!stored) return new Set()
    return new Set(JSON.parse(stored) as string[])
  } catch {
    return new Set()
  }
}

function loadString(key: string, fallback: string): string {
  return localStorage.getItem(key) ?? fallback
}

const defaultPage = TOOL_GROUPS[0]?.pages[0]?.id ?? 'mythic-2e'
const activePage = ref<string>(loadString(STORAGE_KEY_PAGE, defaultPage))
const sidebarOpen = ref<boolean>(loadBoolean(STORAGE_KEY_SIDEBAR, true))
const collapsedGroups = ref<Set<string>>(loadStringSet(STORAGE_KEY_COLLAPSED))

watch(activePage, (val) => localStorage.setItem(STORAGE_KEY_PAGE, val))
watch(sidebarOpen, (val) => localStorage.setItem(STORAGE_KEY_SIDEBAR, String(val)))
watch(collapsedGroups, (val) => localStorage.setItem(STORAGE_KEY_COLLAPSED, JSON.stringify([...val])), { deep: true })

export function useToolNav() {
  function selectPage(id: string) {
    activePage.value = id
  }

  function toggleSidebar() {
    sidebarOpen.value = !sidebarOpen.value
  }

  function toggleGroup(id: string) {
    const next = new Set(collapsedGroups.value)
    if (next.has(id)) {
      next.delete(id)
    } else {
      next.add(id)
    }
    collapsedGroups.value = next
  }

  function isGroupCollapsed(id: string): boolean {
    return collapsedGroups.value.has(id)
  }

  return {
    activePage,
    sidebarOpen,
    collapsedGroups,
    groups: TOOL_GROUPS,
    selectPage,
    toggleSidebar,
    toggleGroup,
    isGroupCollapsed,
  }
}
