import { Node, mergeAttributes } from '@tiptap/core'
import { VueNodeViewRenderer } from '@tiptap/vue-3'
import RollTableNodeView from './RollTableNodeView.vue'

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
    return VueNodeViewRenderer(RollTableNodeView)
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
