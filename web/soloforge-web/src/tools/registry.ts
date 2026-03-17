import type { ToolGroup } from '../types'

export const TOOL_GROUPS: ToolGroup[] = [
  {
    id: 'gm-emulators',
    name: 'GM Emulators',
    sortOrder: 0,
    pages: [
      { id: 'mythic-2e', name: 'Mythic 2e' },
    ],
  },
  {
    id: 'trackers',
    name: 'Trackers',
    sortOrder: 1,
    pages: [
      { id: 'combat-tracker', name: 'Combat Tracker' },
    ],
  },
  {
    id: 'general',
    name: 'General',
    sortOrder: 2,
    pages: [
      { id: 'name-generator', name: 'Name Generator' },
    ],
  },
  {
    id: 'npcs',
    name: 'NPCs',
    sortOrder: 3,
    pages: [
      { id: 'une', name: 'UNE', comingSoon: true },
    ],
  },
  {
    id: 'world-building',
    name: 'World Building',
    sortOrder: 4,
    pages: [
      { id: 'settlements', name: 'Settlements', comingSoon: true },
      { id: 'regions', name: 'Regions', comingSoon: true },
      { id: 'pantheons', name: 'Pantheons', comingSoon: true },
    ],
  },
]

export function findPage(pageId: string) {
  for (const group of TOOL_GROUPS) {
    const page = group.pages.find(p => p.id === pageId)
    if (page) return page
  }
  return null
}
