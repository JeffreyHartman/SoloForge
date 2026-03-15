import { Node, mergeAttributes } from '@tiptap/core'

export const NoteBlockNode = Node.create({
  name: 'noteBlock',
  group: 'block',
  atom: true,
  draggable: false,
  selectable: true,

  addAttributes() {
    return {
      raw: { default: '' },
      noteText: { default: '' },
    }
  },

  parseHTML() {
    return [{
      tag: 'div[data-note-block]',
      getAttrs(dom) {
        const el = dom as HTMLElement
        return {
          raw: el.getAttribute('data-raw') ?? '',
          noteText: el.getAttribute('data-note-text') ?? '',
        }
      },
    }]
  },

  renderHTML({ HTMLAttributes }) {
    return ['div', mergeAttributes(HTMLAttributes, {
      'data-note-block': 'true',
      'data-raw': HTMLAttributes.raw ?? '',
      'data-note-text': HTMLAttributes.noteText ?? '',
    })]
  },

  addNodeView() {
    return ({ node }) => {
      const dom = document.createElement('div')
      dom.contentEditable = 'false'
      dom.className = 'roll-panel-wysiwyg note-block'
      dom.style.borderLeft = '3px solid var(--color-roll-note)'

      const badge = document.createElement('span')
      badge.className = 'roll-panel-wysiwyg-badge'
      badge.style.color = 'var(--color-roll-note-text)'
      badge.style.backgroundColor = 'var(--color-roll-note-bg)'
      badge.textContent = 'Note'
      dom.appendChild(badge)

      const text = document.createElement('span')
      text.className = 'roll-panel-wysiwyg-result'
      text.textContent = node.attrs.noteText
      dom.appendChild(text)

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
