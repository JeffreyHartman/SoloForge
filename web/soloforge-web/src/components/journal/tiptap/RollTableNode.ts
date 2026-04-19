import { Node, mergeAttributes } from '@tiptap/core'
import type { MarkdownToken, MarkdownParseHelpers, JSONContent, MarkdownRendererHelpers, RenderContext } from '@tiptap/core'
import { VueNodeViewRenderer } from '@tiptap/vue-3'
import RollTableNodeView from './RollTableNodeView.vue'

const KNOWN_ROLL_TYPES = new Set([
  'Fate Check',
  'Scene Check',
  'Random Event',
  'Meaning Roll',
  'Dice Roll',
])

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

export const RollTableNode = Node.create({
  name: 'rollTable',
  group: 'block',
  atom: true,
  draggable: false,
  selectable: true,

  addAttributes() {
    return {
      raw: { default: '' },
      rollType: { default: '' },
      fields: { default: '{}' },
    }
  },

  parseHTML() {
    return [{
      tag: 'div[data-roll-table]',
      getAttrs(dom) {
        const el = dom as HTMLElement
        return {
          raw: el.getAttribute('data-raw') ?? '',
          rollType: el.getAttribute('data-roll-type') ?? '',
          fields: el.getAttribute('data-fields') ?? '{}',
        }
      },
    }]
  },

  renderHTML({ HTMLAttributes }) {
    return ['div', mergeAttributes(HTMLAttributes, { 'data-roll-table': 'true' })]
  },

  addStorage() {
    return { enhanced: true }
  },

  addNodeView() {
    return VueNodeViewRenderer(RollTableNodeView)
  },

  markdownTokenizer: {
    name: 'rollTable',
    level: 'block',
    start: (src: string) => {
      const idx = src.indexOf('|')
      return idx >= 0 ? idx : -1
    },
    tokenize(src: string): MarkdownToken | undefined {
      // Match a markdown table starting with | on first line and |---| separator on second
      const match = src.match(/^(\|[^\n]+\|\n\|[\s:|-]+\|\n(?:\|[^\n]+\|\n?)*)/)
      if (!match) return undefined

      const tableRaw = match[1]!.replace(/\n$/, '')
      const parsed = parseTableFields(tableRaw)
      if (!parsed) return undefined

      return {
        type: 'rollTable',
        raw: match[0]!,
        rollType: parsed.rollType,
        fields: JSON.stringify(parsed.fields),
      }
    },
  },

  parseMarkdown(token: MarkdownToken, _helpers: MarkdownParseHelpers) {
    return {
      type: 'rollTable',
      attrs: {
        raw: (token.raw ?? '').replace(/\n$/, ''),
        rollType: token.rollType ?? '',
        fields: token.fields ?? '{}',
      },
    }
  },

  renderMarkdown(node: JSONContent, _helpers: MarkdownRendererHelpers, _ctx: RenderContext) {
    return `${node.attrs?.raw ?? ''}\n`
  },
})
