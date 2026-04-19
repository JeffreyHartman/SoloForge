import { Mark, mergeAttributes, InputRule } from '@tiptap/core'
import type { MarkdownToken, MarkdownParseHelpers, JSONContent, MarkdownRendererHelpers, RenderContext } from '@tiptap/core'

export const WikiLinkMark = Mark.create({
  name: 'wikiLink',

  addAttributes() {
    return {
      path: { default: '' },
    }
  },

  parseHTML() {
    return [{
      tag: 'span[data-wiki-link]',
      getAttrs(dom) {
        const el = dom as HTMLElement
        return {
          path: el.getAttribute('data-path') ?? '',
        }
      },
    }]
  },

  renderHTML({ HTMLAttributes }) {
    const { path, ...rest } = HTMLAttributes
    return [
      'span',
      mergeAttributes(rest, {
        'data-wiki-link': 'true',
        'data-path': path,
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

          const mark = this.type.create({ path: resolvedPath })
          const { tr } = state
          tr.replaceWith(range.from, range.to, state.schema.text(displayPart, [mark]))
        },
      }),
    ]
  },

  markdownTokenizer: {
    name: 'wikiLink',
    level: 'inline',
    start: (src: string) => {
      const idx = src.indexOf('[[')
      return idx >= 0 ? idx : -1
    },
    tokenize(src: string): MarkdownToken | undefined {
      const match = src.match(/^\[\[([^\]\n]+)\]\]/)
      if (!match) return undefined

      const inner = (match[1] ?? '').trim()
      if (!inner) return undefined

      const pipeIdx = inner.indexOf('|')
      const pathPart = pipeIdx >= 0 ? inner.substring(0, pipeIdx).trim() : inner
      const displayPart = pipeIdx >= 0 ? inner.substring(pipeIdx + 1).trim() : inner
      const resolvedPath = pathPart.endsWith('.md') ? pathPart : `${pathPart}.md`

      return {
        type: 'wikiLink',
        raw: match[0]!,
        path: resolvedPath,
        display: displayPart,
      }
    },
  },

  parseMarkdown(token: MarkdownToken, helpers: MarkdownParseHelpers) {
    return helpers.applyMark('wikiLink', [
      helpers.createTextNode(token.display ?? token.text ?? ''),
    ], { path: token.path ?? '' })
  },

  renderMarkdown(node: JSONContent, helpers: MarkdownRendererHelpers, _ctx: RenderContext) {
    // For marks, the MarkdownManager calls renderMarkdown with a synthetic node
    // containing a placeholder. It splits the result around the placeholder to
    // extract the opening/closing syntax. We use renderChildren() so the
    // placeholder passes through correctly.
    //
    // The opening is always `[[` and closing is always `]]`. The path is stored
    // in the mark attrs, and we only include it in the output when it differs
    // from the display text (which the MarkdownManager inserts automatically).
    // However, since the placeholder makes it impossible to compare display vs
    // path at render time, we always use the simple form `[[display]]` and let
    // the parser resolve the path on re-import.
    const inner = helpers.renderChildren(node)
    return `[[${inner}]]`
  },
})
