import { computed, type Ref } from 'vue'

export interface TextSegment {
  id: string
  type: 'text'
  raw: string
}

export interface RollSegment {
  id: string
  type: 'roll'
  rollType: string
  fields: Record<string, string>
  raw: string
}

export type JournalSegment = TextSegment | RollSegment

const KNOWN_ROLL_TYPES = new Set([
  'Fate Check',
  'Scene Check',
  'Random Event',
  'Meaning Roll',
  'Dice Roll',
])

function hashId(raw: string, index: number): string {
  let hash = 0
  const str = raw.slice(0, 120) + ':' + index
  for (let i = 0; i < str.length; i++) {
    hash = ((hash << 5) - hash) + str.charCodeAt(i)
    hash |= 0
  }
  return 's' + Math.abs(hash).toString(36)
}

function parseTableFields(tableText: string): { rollType: string; fields: Record<string, string> } | null {
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

  return { rollType, fields }
}

function parseSegments(text: string): JournalSegment[] {
  if (!text.trim()) return []

  const lines = text.split('\n')
  const segments: JournalSegment[] = []
  let textLines: string[] = []
  let segIndex = 0
  let i = 0

  function flushText() {
    const raw = textLines.join('\n')
    if (raw.trim()) {
      segments.push({ id: hashId(raw, segIndex++), type: 'text', raw })
    }
    textLines = []
  }

  while (i < lines.length) {
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
      })
      i++
      continue
    }

    // Table block: line starts with | and next line is a separator row
    if (/^\|/.test(line) && i + 1 < lines.length && /^\|[\s:|-]+\|/.test(lines[i + 1])) {
      const tableLines: string[] = []
      const tableStart = i
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

    const idx = text.indexOf(seg.raw)
    if (idx === -1) return

    let start = idx
    let end = idx + seg.raw.length

    // Consume trailing newlines
    while (end < text.length && text[end] === '\n') end++
    // Consume one leading newline if not at start
    if (start > 0 && text[start - 1] === '\n') start--

    let updated = text.slice(0, Math.max(0, start)) + text.slice(end)
    content.value = updated.replace(/\n{3,}/g, '\n\n')
  }

  return { segments, deleteSegment }
}
