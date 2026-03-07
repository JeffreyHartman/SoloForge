import { ref, computed, reactive } from 'vue'
import type { TableInfo, TableGroup, QuickSet } from '../types'
import { apiGet } from './useApi'

const tables = ref<TableInfo[]>([])
const quickSets = ref<QuickSet[]>([])

const loading = reactive({
  tables: false,
  quickSets: false,
})

export function useTables() {
  const tableGroups = computed<TableGroup[]>(() => {
    const byCategory = new Map<string, TableInfo[]>()
    for (const t of tables.value) {
      const cat = t.category || (t.isElement ? 'Elements' : 'Core')
      const arr = byCategory.get(cat) ?? []
      arr.push(t)
      byCategory.set(cat, arr)
    }

    return Array.from(byCategory.entries())
      .map(([label, items]) => ({
        label,
        items: items.slice().sort((a, b) => a.displayName.localeCompare(b.displayName)),
      }))
      .sort((a, b) => a.label.localeCompare(b.label))
  })

  async function refreshTables() {
    loading.tables = true
    try {
      tables.value = await apiGet<TableInfo[]>('/api/tables')
    } finally {
      loading.tables = false
    }
  }

  async function refreshQuickSets() {
    loading.quickSets = true
    try {
      quickSets.value = await apiGet<QuickSet[]>('/api/quick-sets')
    } finally {
      loading.quickSets = false
    }
  }

  function getFirstElementTable(): string {
    const first = tables.value.find(t => t.isElement) ?? tables.value[0]
    return first?.id ?? ''
  }

  function getDefaultFusionTables(): [string, string] {
    const first = tables.value[0]?.id ?? ''
    const second = tables.value[1]?.id ?? tables.value[0]?.id ?? ''
    return [first, second]
  }

  function getFirstQuickSetId(): string {
    return quickSets.value[0]?.id ?? ''
  }

  return {
    tables,
    quickSets,
    tableGroups,
    loading,
    refreshTables,
    refreshQuickSets,
    getFirstElementTable,
    getDefaultFusionTables,
    getFirstQuickSetId,
  }
}
