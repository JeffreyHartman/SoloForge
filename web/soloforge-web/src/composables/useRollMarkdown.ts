import type {
  FateCheckResponse,
  SceneCheckResponse,
  RandomEventResult,
  MeaningResult,
  QuickSetResult,
  DiceRollResponse,
} from '../types'

/** Formats a fate check result as a markdown table for clipboard copy. */
export function fateCheckToMarkdown(result: FateCheckResponse): string {
  let md = `| Fate Check | &nbsp; |\n| ---------- | ------ |\n`
  if (result.fate) {
    md += `| **Question** | ${result.odds} |\n`
    md += `| **Result** | ${result.fate.result} |\n`
    md += `| *Details* | Odds: ${result.odds}, Roll: ${result.fate.roll}, Chaos: ${result.chaos} |\n`
  }
  return md.trimEnd()
}

/** Formats a scene check result as a markdown table for clipboard copy. */
export function sceneCheckToMarkdown(result: SceneCheckResponse): string {
  let md = `| Scene Check | &nbsp; |\n| ----------- | ------ |\n`
  md += `| **Result** | ${result.scene.result} |\n`
  const details = [`Roll: ${result.scene.roll}`, `Chaos: ${result.chaos}`]
  if (result.scene.sceneAdjustment) details.push(`Adjustment: ${result.scene.sceneAdjustment}`)
  md += `| *Details* | ${details.join(', ')} |\n`
  return md.trimEnd()
}

/** Formats a random event result as a markdown table for clipboard copy. */
export function randomEventToMarkdown(result: RandomEventResult): string {
  let md = `| Random Event | &nbsp; |\n| ------------ | ------ |\n`
  md += `| **Event** | ${result.eventFocus}: ${result.eventAction} |\n`
  const details: string[] = []
  if (result.selectedCharacter) details.push(`Character: ${result.selectedCharacter}`)
  if (result.selectedThread) details.push(`Thread: ${result.selectedThread}`)
  if (details.length > 0) md += `| *Details* | ${details.join(', ')} |\n`
  return md.trimEnd()
}

/** Formats a meaning roll result as a markdown table for clipboard copy. */
export function meaningToMarkdown(result: MeaningResult): string {
  let md = `| Meaning Roll | &nbsp; |\n| ------------ | ------ |\n`
  md += `| **Result** | ${result.combined} |\n`
  md += `| *Details* | Table: ${result.tableName} |\n`
  return md.trimEnd()
}

/** Formats a quick set generation result as a markdown table for clipboard copy. */
export function quickSetToMarkdown(result: QuickSetResult): string {
  let md = `| Quick Set | &nbsp; |\n| --------- | ------ |\n`
  md += `| **Result** | ${result.quickSet.name} Generated |\n`
  const details = result.results.map(r => `${r.label}: ${r.combined}`).join('<br>')
  md += `| *Details* | ${details} |\n`
  return md.trimEnd()
}

/** Formats a dice roll result as a markdown table for clipboard copy. */
export function diceRollToMarkdown(result: DiceRollResponse): string {
  let md = `| Dice Roll | &nbsp; |\n| --------- | ------ |\n`
  md += `| **Expression** | ${result.roll.summary} |\n`
  md += `| **Total** | ${result.roll.total} |\n`
  if (result.breakdown) md += `| *Details* | ${result.breakdown} |\n`
  return md.trimEnd()
}

/** Copies text to the system clipboard. Returns true on success, false on failure. */
export async function copyToClipboard(text: string): Promise<boolean> {
  try {
    await navigator.clipboard.writeText(text)
    return true
  } catch {
    return false
  }
}
