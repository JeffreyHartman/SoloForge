export type ToolExecution = 'modal' | 'instant'

export interface PinnableTool {
  id: string
  label: string
  execution: ToolExecution
  pinnable: boolean
  expandsInToolbar?: boolean
  subActions?: { id: string; label: string; expression: string }[]
}

export const PINNABLE_TOOLS: PinnableTool[] = [
  { id: 'fate-check', label: 'Fate Check', execution: 'modal', pinnable: true },
  { id: 'scene-check', label: 'Scene Check', execution: 'modal', pinnable: true },
  { id: 'random-event', label: 'Random Event', execution: 'instant', pinnable: true },
  { id: 'meaning', label: 'Meaning', execution: 'modal', pinnable: true },
  {
    id: 'dice-roller',
    label: 'Dice Roller',
    execution: 'modal',
    pinnable: true,
    expandsInToolbar: true,
    subActions: [
      { id: 'd4', label: 'd4', expression: '1d4' },
      { id: 'd6', label: 'd6', expression: '1d6' },
      { id: 'd8', label: 'd8', expression: '1d8' },
      { id: 'd10', label: 'd10', expression: '1d10' },
      { id: 'd12', label: 'd12', expression: '1d12' },
      { id: 'd20', label: 'd20', expression: '1d20' },
      { id: 'd100', label: 'd100', expression: '1d100' },
    ],
  },
  { id: 'name-generator', label: 'Name Generator', execution: 'modal', pinnable: false },
]

export function getPinnableTool(id: string): PinnableTool | undefined {
  return PINNABLE_TOOLS.find(t => t.id === id)
}

export function getPinnableToolIds(): string[] {
  return PINNABLE_TOOLS.filter(t => t.pinnable).map(t => t.id)
}
