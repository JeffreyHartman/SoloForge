import { Node, mergeAttributes } from '@tiptap/core'
import type { MarkdownToken, MarkdownParseHelpers, JSONContent, MarkdownRendererHelpers, RenderContext } from '@tiptap/core'

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

  addStorage() {
    return { enhanced: true }
  },

  addNodeView() {
    return ({ node, editor }) => {
      const enhanced = (editor.storage as any).noteBlock?.enhanced ?? true

      const dom = document.createElement('div')
      dom.contentEditable = 'false'

      if (!enhanced) {
        dom.className = 'roll-panel-wysiwyg-raw'
        const pre = document.createElement('pre')
        pre.textContent = node.attrs.raw
        dom.appendChild(pre)
        return { dom }
      }

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

  markdownTokenizer: {
    name: 'noteBlock',
    level: 'block',
    start: (src: string) => {
      const match = src.match(/^>\s*\*\*Note:\*\*/m)
      return match ? (match.index ?? -1) : -1
    },
    tokenize(src: string): MarkdownToken | undefined {
      // Match > **Note:** on first line, then collect continuation > lines
      const firstLineMatch = src.match(/^>\s*\*\*Note:\*\*[^\n]*/)
      if (!firstLineMatch) return undefined

      let raw = firstLineMatch[0]!
      let rest = src.slice(raw.length)

      // Collect continuation lines starting with >
      while (rest.startsWith('\n')) {
        const nextLine = rest.slice(1).match(/^>[^\n]*/)
        if (!nextLine) break
        // Stop if next line starts a new note block
        if (/^>\s*\*\*Note:\*\*/.test(nextLine[0]!)) break
        raw += '\n' + nextLine[0]!
        rest = rest.slice(1 + nextLine[0]!.length)
      }

      // Extract note text: strip > prefix and **Note:** from first line,
      // strip > prefix from continuation lines
      const lines = raw.split('\n')
      const firstText = (lines[0] ?? '').replace(/^>\s*\*\*Note:\*\*\s*/, '')
      const continuationTexts = lines.slice(1).map(line => line.replace(/^>\s?/, ''))
      const noteText = [firstText, ...continuationTexts].join('\n').trim()

      return {
        type: 'noteBlock',
        raw: raw + '\n',
        noteText,
      }
    },
  },

  parseMarkdown(token: MarkdownToken, _helpers: MarkdownParseHelpers) {
    return {
      type: 'noteBlock',
      attrs: {
        raw: (token.raw ?? '').replace(/\n$/, ''),
        noteText: token.noteText ?? '',
      },
    }
  },

  renderMarkdown(node: JSONContent, _helpers: MarkdownRendererHelpers, _ctx: RenderContext) {
    return `${node.attrs?.raw ?? ''}\n`
  },
})
