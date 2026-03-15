import { computed, type Ref } from 'vue'

export type RollType = 'Fate Check' | 'Scene Check' | 'Random Event' | 'Meaning Roll' | 'Dice Roll' | 'Note'

export interface TextSegment {
  id: string
  type: 'text'
  raw: string
  offset: number
}

export interface RollSegment {
  id: string
  type: 'roll'
  rollType: RollType
  fields: Record<string, string>
  raw: string
  offset: number
}

export type JournalSegment = TextSegment | RollSegment

const KNOWN_ROLL_TYPES: ReadonlySet<string> = new Set<RollType>([
  'Fate Check',
  'Scene Check',
  'Random Event',
  'Meaning Roll',
  'Dice Roll',
])

export function hashId(raw: string, index: number): string {
  let hash = 0
  const str = raw.slice(0, 120) + ':' + index
  for (let i = 0; i < str.length; i++) {
    hash = ((hash << 5) - hash) + str.charCodeAt(i)
    hash |= 0
  }
  return 's' + Math.abs(hash).toString(36)
}

export function parseTableFields(tableText: string): { rollType: RollType; fields: Record<string, string> } | null {
  const lines = tableText.split('\n')
  if (lines.length < 3) return null

  const headerMatch = lines[0].match(/^\|\s*(.+?)\s*\|/)
  if (!headerMatch) return null

  const rollType = headerMatch[1].replace(/&nbsp;/g, '').replace(/\s+/g, ' ').trim()
  if (!KNOWN_ROLL_TYPES.has(rollType)) return null

  const fields: Record<string, string> = {}
  for (let i = 2; i < lines.length; i++) {
    const cellMatch = lines[i].match(/^\|\s*\*{1,2}([^*]+)\*{1,2}\s*\|\s*(.*?)\s*\|/)
    if (cellMatch) {
      fields[cellMatch[1].trim()] = cellMatch[2].trim()
    }
  }

  return { rollType: rollType as RollType, fields }
}

export function parseSegments(text: string): JournalSegment[] {
  if (!text.trim()) return []

  const lines = text.split('\n')
  const segments: JournalSegment[] = []
  let textLines: string[] = []
  let textStartOffset = 0
  let segIndex = 0
  let i = 0

  // Pre-compute byte offset of each line
  const lineOffsets: number[] = []
  let pos = 0
  for (const line of lines) {
    lineOffsets.push(pos)
    pos += line.length + 1 // +1 for \n
  }

  function flushText() {
    const raw = textLines.join('\n')
    if (raw.trim()) {
      segments.push({ id: hashId(raw, segIndex++), type: 'text', raw, offset: textStartOffset })
    }
    textLines = []
  }

  while (i < lines.length) {
    if (textLines.length === 0) textStartOffset = lineOffsets[i]
    const line = lines[i]

    // Note blockquote
    if (/^>\s*\*\*Note:\*\*/.test(line)) {
      flushText()
      segments.push({
        id: hashId(line, segIndex++),
        type: 'roll',
        rollType: 'Note',
        fields: { Note: line.replace(/^>\s*\*\*Note:\*\*\s*/, '') },
        raw: line,
        offset: lineOffsets[i],
      })
      i++
      continue
    }

    // Table block: line starts with | and next line is a separator row
    if (/^\|/.test(line) && i + 1 < lines.length && /^\|[\s:|-]+\|/.test(lines[i + 1])) {
      const tableOffset = lineOffsets[i]
      const tableLines: string[] = []
      while (i < lines.length && /^\|/.test(lines[i])) {
        tableLines.push(lines[i])
        i++
      }

      const tableRaw = tableLines.join('\n')
      const parsed = parseTableFields(tableRaw)

      if (parsed) {
        flushText()
        segments.push({
          id: hashId(tableRaw, segIndex++),
          type: 'roll',
          rollType: parsed.rollType,
          fields: parsed.fields,
          raw: tableRaw,
          offset: tableOffset,
        })
      } else {
        // Not a recognized roll table — keep as text
        textLines.push(...tableLines)
      }
      continue
    }

    textLines.push(line)
    i++
  }

  flushText()
  return segments
}

export function useJournalParser(content: Ref<string | undefined>) {
  const segments = computed(() => parseSegments(content.value ?? ''))

  function deleteSegment(segmentId: string) {
    const text = content.value
    if (!text) return

    const seg = segments.value.find(s => s.id === segmentId)
    if (!seg) return

    // Use the tracked offset for precise removal
    let start = seg.offset
    let end = seg.offset + seg.raw.length

    // Verify the offset still matches (content may have shifted)
    if (text.slice(start, end) !== seg.raw) return

    const before = text.slice(0, start).replace(/\n+$/, '')
    const after = text.slice(end).replace(/^\n+/, '')
    content.value = before && after ? `${before}\n\n${after}` : before + after
  }

  return { segments, deleteSegment }
}
