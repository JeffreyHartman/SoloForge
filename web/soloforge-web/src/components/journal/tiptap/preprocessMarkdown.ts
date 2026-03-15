/**
 * Preprocesses raw markdown before loading into Tiptap. Replaces roll tables,
 * note blocks, and wiki-links with HTML elements that custom Tiptap extensions
 * can parse via their parseHTML rules.
 */

const KNOWN_ROLL_TYPES = new Set([
  'Fate Check',
  'Scene Check',
  'Random Event',
  'Meaning Roll',
  'Dice Roll',
])

export function encodeAttr(s: string): string {
  return s
    .replace(/&/g, '&amp;')
    .replace(/"/g, '&quot;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/\n/g, '&#10;')
}

export function parseTableFields(tableText: string): { rollType: string; fields: Record<string, string> } | null {
  const lines = tableText.split('\n')
  if (lines.length < 3) return null

  const headerMatch = (lines[0] ?? '').match(/^\|\s*(.+?)\s*\|/)
  if (!headerMatch) return null

  const rollType = (headerMatch[1] ?? '').replace(/&nbsp;/g, '').replace(/\s+/g, ' ').trim()
  if (!KNOWN_ROLL_TYPES.has(rollType)) return null

  const fields: Record<string, string> = {}
  for (let i = 2; i < lines.length; i++) {
    const cellMatch = (lines[i] ?? '').match(/^\|\s*\*{1,2}([^*]+)\*{1,2}\s*\|\s*(.*?)\s*\|/)
    if (cellMatch) {
      fields[(cellMatch[1] ?? '').trim()] = (cellMatch[2] ?? '').trim()
    }
  }

  return { rollType, fields }
}

export function preprocessForWysiwyg(markdown: string): string {
  if (!markdown) return ''

  // Preserve extra blank lines by converting them to <p></p> tags.
  // markdown-it collapses consecutive blank lines into a single paragraph
  // break, so we replace each extra blank line with an HTML empty paragraph
  // that tiptap will parse as an empty paragraph node.
  markdown = markdown.replace(/\n{3,}/g, (match) => {
    // N newlines = N-1 line breaks. Standard paragraph break uses 2 newlines
    // (1 blank line). Each additional blank line needs an empty <p></p>.
    const extraBlanks = match.length - 2
    return '\n\n' + '<p></p>\n\n'.repeat(extraBlanks)
  })

  const lines = markdown.split('\n')
  const result: string[] = []
  let i = 0

  while (i < lines.length) {
    const line = lines[i] ?? ''

    // Note blockquote: > **Note:** ...
    if (/^>\s*\*\*Note:\*\*/.test(line)) {
      const noteText = line.replace(/^>\s*\*\*Note:\*\*\s*/, '')
      result.push(
        `<div data-note-block="true" data-raw="${encodeAttr(line)}" data-note-text="${encodeAttr(noteText)}"></div>`,
      )
      i++
      continue
    }

    // Roll table: line starts with | and next line is separator
    if (/^\|/.test(line) && i + 1 < lines.length && /^\|[\s:|-]+\|/.test(lines[i + 1] ?? '')) {
      const tableLines: string[] = []
      while (i < lines.length && /^\|/.test(lines[i] ?? '')) {
        tableLines.push(lines[i] ?? '')
        i++
      }
      const tableRaw = tableLines.join('\n')
      const parsed = parseTableFields(tableRaw)

      if (parsed) {
        result.push(
          `<div data-roll-table="true" data-raw="${encodeAttr(tableRaw)}" data-roll-type="${encodeAttr(parsed.rollType)}" data-fields="${encodeAttr(JSON.stringify(parsed.fields))}"></div>`,
        )
        continue
      }

      // Not a recognized roll table — keep as markdown
      result.push(...tableLines)
      continue
    }

    // Wiki-links: [[path]] or [[path|display]]
    if (line.includes('[[')) {
      result.push(
        line.replace(/\[\[([^\]]+)\]\]/g, (_match, inner: string) => {
          const raw = inner.trim()
          if (!raw || raw.includes('\n')) return _match

          const pipeIdx = raw.indexOf('|')
          const pathPart = pipeIdx >= 0 ? raw.substring(0, pipeIdx).trim() : raw
          const displayPart = pipeIdx >= 0 ? raw.substring(pipeIdx + 1).trim() : raw
          const resolvedPath = pathPart.endsWith('.md') ? pathPart : `${pathPart}.md`

          return `<span data-wiki-link="true" data-path="${encodeAttr(resolvedPath)}" data-raw="${encodeAttr(raw)}">${encodeAttr(displayPart)}</span>`
        }),
      )
      i++
      continue
    }

    result.push(line)
    i++
  }

  return result.join('\n')
}
