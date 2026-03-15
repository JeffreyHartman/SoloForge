import { Node, mergeAttributes } from '@tiptap/core'

interface RollStyle {
  border: string
  color: string
  bg: string
  label: string
}

const DEFAULT_STYLE: RollStyle = { border: 'var(--color-roll-fate)', color: 'var(--color-roll-fate)', bg: 'var(--color-roll-fate-bg)', label: 'Roll' }

const STYLES: Record<string, RollStyle> = {
  'Fate Check':   { border: 'var(--color-roll-fate)',    color: 'var(--color-roll-fate)',         bg: 'var(--color-roll-fate-bg)',    label: 'Fate' },
  'Scene Check':  { border: 'var(--color-roll-scene)',   color: 'var(--color-roll-scene-text)',   bg: 'var(--color-roll-scene-bg)',   label: 'Scene' },
  'Random Event': { border: 'var(--color-roll-event)',   color: 'var(--color-roll-event-text)',   bg: 'var(--color-roll-event-bg)',   label: 'Event' },
  'Meaning Roll': { border: 'var(--color-roll-meaning)', color: 'var(--color-roll-meaning-text)', bg: 'var(--color-roll-meaning-bg)', label: 'Meaning' },
  'Dice Roll':    { border: 'var(--color-roll-dice)',    color: 'var(--color-roll-dice-text)',    bg: 'var(--color-roll-dice-bg)',    label: 'Dice' },
}

function getSummary(rollType: string, fields: Record<string, string>) {
  const result = fields.Result ?? ''
  if (rollType === 'Fate Check')   return { context: fields.Question ?? '', result }
  if (rollType === 'Scene Check')  return { context: fields.Context ?? '', result }
  if (rollType === 'Meaning Roll') return { context: fields.For ?? '', result }
  if (rollType === 'Dice Roll')    return { context: fields.Expression ?? '', result: fields.Total ?? result }
  if (rollType === 'Random Event') return { context: '', result: fields.Event ?? result }
  return { context: '', result: Object.values(fields)[0] ?? '' }
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

  addNodeView() {
    return ({ node }) => {
      const rollType = node.attrs.rollType as string
      let fields: Record<string, string> = {}
      try {
        fields = JSON.parse(node.attrs.fields || '{}') as Record<string, string>
      } catch {
        console.warn('RollTableNode: Failed to parse fields JSON', node.attrs.fields)
      }
      const style = STYLES[rollType] ?? DEFAULT_STYLE
      const summary = getSummary(rollType, fields)

      const dom = document.createElement('div')
      dom.contentEditable = 'false'
      dom.className = 'roll-panel-wysiwyg'
      dom.style.borderLeft = `3px solid ${style.border}`

      // Badge
      const badge = document.createElement('span')
      badge.className = 'roll-panel-wysiwyg-badge'
      badge.style.color = style.color
      badge.style.backgroundColor = style.bg
      badge.textContent = style.label
      dom.appendChild(badge)

      // Context
      if (summary.context) {
        const ctx = document.createElement('span')
        ctx.className = 'roll-panel-wysiwyg-context'
        ctx.textContent = summary.context
        dom.appendChild(ctx)
      }

      // Result
      if (summary.result) {
        const res = document.createElement('span')
        res.className = 'roll-panel-wysiwyg-result'
        res.textContent = summary.result
        dom.appendChild(res)
      }

      return { dom }
    }
  },

  addStorage() {
    return {
      markdown: {
        serialize(state: any, node: any) {
          state.write(node.attrs.raw)
          state.ensureNewLine()
          state.write('\n')
        },
        parse: {},
      },
    }
  },
})
