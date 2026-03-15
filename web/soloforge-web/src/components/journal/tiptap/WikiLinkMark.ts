import { Mark, mergeAttributes } from '@tiptap/core'
import { InputRule } from '@tiptap/core'

export const WikiLinkMark = Mark.create({
  name: 'wikiLink',

  addAttributes() {
    return {
      path: { default: '' },
      raw: { default: '' },
    }
  },

  parseHTML() {
    return [{
      tag: 'span[data-wiki-link]',
      getAttrs(dom) {
        const el = dom as HTMLElement
        return {
          path: el.getAttribute('data-path') ?? '',
          raw: el.getAttribute('data-raw') ?? '',
        }
      },
    }]
  },

  renderHTML({ HTMLAttributes }) {
    const { path, raw, ...rest } = HTMLAttributes
    return [
      'span',
      mergeAttributes(rest, {
        'data-wiki-link': 'true',
        'data-path': path,
        'data-raw': raw,
        class: 'wiki-link',
        role: 'link',
        tabindex: '0',
      }),
      0,
    ]
  },

  addInputRules() {
    return [
      new InputRule({
        find: /\[\[([^\]]+)\]\]$/,
        handler: ({ state, range, match }) => {
          const inner = (match[1] ?? '').trim()
          if (!inner) return

          const pipeIdx = inner.indexOf('|')
          const pathPart = pipeIdx >= 0 ? inner.substring(0, pipeIdx).trim() : inner
          const displayPart = pipeIdx >= 0 ? inner.substring(pipeIdx + 1).trim() : inner
          const resolvedPath = pathPart.endsWith('.md') ? pathPart : `${pathPart}.md`

          const mark = this.type.create({ path: resolvedPath, raw: inner })
          const { tr } = state
          tr.replaceWith(range.from, range.to, state.schema.text(displayPart, [mark]))
        },
      }),
    ]
  },

  addStorage() {
    return {
      markdown: {
        serialize: {
          open(_state: any, mark: any) {
            const raw: string = mark.attrs.raw ?? ''
            const pipeIdx = raw.indexOf('|')
            if (pipeIdx >= 0) {
              return `[[${raw.substring(0, pipeIdx).trim()}|`
            }
            return '[['
          },
          close: ']]',
          mixable: false,
          expelEnclosingWhitespace: true,
        },
        parse: {},
      },
    }
  },
})
