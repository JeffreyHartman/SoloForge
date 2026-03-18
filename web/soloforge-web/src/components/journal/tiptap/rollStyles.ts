export interface RollStyle {
  border: string
  color: string
  bg: string
  label: string
}

export const DEFAULT_STYLE: RollStyle = {
  border: 'var(--color-roll-fate)',
  color: 'var(--color-roll-fate)',
  bg: 'var(--color-roll-fate-bg)',
  label: 'Roll',
}

export const STYLES: Record<string, RollStyle> = {
  'Fate Check':   { border: 'var(--color-roll-fate)',    color: 'var(--color-roll-fate)',         bg: 'var(--color-roll-fate-bg)',    label: 'Fate' },
  'Scene Check':  { border: 'var(--color-roll-scene)',   color: 'var(--color-roll-scene-text)',   bg: 'var(--color-roll-scene-bg)',   label: 'Scene' },
  'Random Event': { border: 'var(--color-roll-event)',   color: 'var(--color-roll-event-text)',   bg: 'var(--color-roll-event-bg)',   label: 'Event' },
  'Meaning Roll': { border: 'var(--color-roll-meaning)', color: 'var(--color-roll-meaning-text)', bg: 'var(--color-roll-meaning-bg)', label: 'Meaning' },
  'Dice Roll':    { border: 'var(--color-roll-dice)',    color: 'var(--color-roll-dice-text)',    bg: 'var(--color-roll-dice-bg)',    label: 'Dice' },
}

export function getSummary(rollType: string, fields: Record<string, string>) {
  const result = fields.Result ?? ''
  if (rollType === 'Fate Check')   return { context: fields.Question ?? '', result }
  if (rollType === 'Scene Check')  return { context: fields.Context ?? '', result }
  if (rollType === 'Meaning Roll') return { context: fields.For ?? '', result }
  if (rollType === 'Dice Roll')    return { context: fields.Expression ?? '', result: fields.Total ?? result }
  if (rollType === 'Random Event') return { context: '', result: fields.Event ?? result }
  return { context: '', result: Object.values(fields)[0] ?? '' }
}
