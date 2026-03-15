import { describe, it, expect, vi, beforeEach } from 'vitest'

vi.mock('../useApi', () => ({
  apiGet: vi.fn(),
}))

import { useTables } from '../useTables'
import type { TableInfo } from '../../types'

describe('useTables', () => {
  beforeEach(() => {
    // Reset the module-level tables ref
    const { tables, quickSets } = useTables()
    tables.value = []
    quickSets.value = []
  })

  describe('tableGroups', () => {
    it('groups tables by category and sorts', () => {
      const { tables, tableGroups } = useTables()
      tables.value = [
        { id: '1', displayName: 'Bravo', isElement: false, category: 'Core' },
        { id: '2', displayName: 'Alpha', isElement: false, category: 'Core' },
        { id: '3', displayName: 'Charlie', isElement: true, category: 'Elements' },
      ] as TableInfo[]

      expect(tableGroups.value).toHaveLength(2)
      // Categories sorted alphabetically
      expect(tableGroups.value[0].label).toBe('Core')
      expect(tableGroups.value[1].label).toBe('Elements')
      // Items sorted by displayName within category
      expect(tableGroups.value[0].items[0].displayName).toBe('Alpha')
      expect(tableGroups.value[0].items[1].displayName).toBe('Bravo')
    })

    it('uses fallback category based on isElement', () => {
      const { tables, tableGroups } = useTables()
      tables.value = [
        { id: '1', displayName: 'Test', isElement: true, category: '' },
        { id: '2', displayName: 'Core Table', isElement: false, category: '' },
      ] as TableInfo[]

      const labels = tableGroups.value.map(g => g.label)
      expect(labels).toContain('Elements')
      expect(labels).toContain('Core')
    })
  })

  describe('getFirstElementTable', () => {
    it('returns first element table ID', () => {
      const { tables, getFirstElementTable } = useTables()
      tables.value = [
        { id: 'core1', displayName: 'C', isElement: false, category: 'Core' },
        { id: 'elem1', displayName: 'E', isElement: true, category: 'Elements' },
      ] as TableInfo[]
      expect(getFirstElementTable()).toBe('elem1')
    })

    it('falls back to first table when no element tables', () => {
      const { tables, getFirstElementTable } = useTables()
      tables.value = [
        { id: 'core1', displayName: 'C', isElement: false, category: 'Core' },
      ] as TableInfo[]
      expect(getFirstElementTable()).toBe('core1')
    })

    it('returns empty string when no tables', () => {
      const { getFirstElementTable } = useTables()
      expect(getFirstElementTable()).toBe('')
    })
  })

  describe('getDefaultFusionTables', () => {
    it('returns first two table IDs', () => {
      const { tables, getDefaultFusionTables } = useTables()
      tables.value = [
        { id: 'a', displayName: 'A', isElement: false, category: '' },
        { id: 'b', displayName: 'B', isElement: false, category: '' },
        { id: 'c', displayName: 'C', isElement: false, category: '' },
      ] as TableInfo[]
      expect(getDefaultFusionTables()).toEqual(['a', 'b'])
    })

    it('duplicates first ID when only one table', () => {
      const { tables, getDefaultFusionTables } = useTables()
      tables.value = [
        { id: 'only', displayName: 'Only', isElement: false, category: '' },
      ] as TableInfo[]
      expect(getDefaultFusionTables()).toEqual(['only', 'only'])
    })

    it('returns empty strings when no tables', () => {
      const { getDefaultFusionTables } = useTables()
      expect(getDefaultFusionTables()).toEqual(['', ''])
    })
  })

  describe('getFirstQuickSetId', () => {
    it('returns first quick set ID', () => {
      const { quickSets, getFirstQuickSetId } = useTables()
      quickSets.value = [{ id: 'qs1', name: 'Test', description: '', steps: [] }]
      expect(getFirstQuickSetId()).toBe('qs1')
    })

    it('returns empty string when no quick sets', () => {
      const { getFirstQuickSetId } = useTables()
      expect(getFirstQuickSetId()).toBe('')
    })
  })
})
