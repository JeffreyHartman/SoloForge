import { describe, it } from 'vitest'
import { Editor } from '@tiptap/core'
import StarterKit from '@tiptap/starter-kit'
import { Table, TableRow, TableCell, TableHeader } from '@tiptap/extension-table'
import { Markdown } from '@tiptap/markdown'
import { RollTableNode } from '../RollTableNode'
import { NoteBlockNode } from '../NoteBlockNode'
import { WikiLinkMark } from '../WikiLinkMark'

function makeEditor(opts: any) {
  return new Editor({
    content: 'hello',
    contentType: 'markdown',
    extensions: [
      StarterKit,
      Table.configure({ resizable: false }),
      TableRow, TableCell, TableHeader,
      Markdown.configure({ markedOptions: opts }),
      RollTableNode, NoteBlockNode, WikiLinkMark,
    ],
  })
}

describe('debug pipe in wiki-link', () => {
  it('gfm:true, breaks:true', () => {
    const ed = makeEditor({ gfm: true, breaks: true })
    const tokens = ed.storage.markdown.manager.instance.lexer('See [[some/path|Custom Display]] here')
    console.log('gfm+breaks raw:', JSON.stringify(tokens[0]?.raw))
    ed.destroy()
  })

  it('gfm:true, breaks:false', () => {
    const ed = makeEditor({ gfm: true, breaks: false })
    const tokens = ed.storage.markdown.manager.instance.lexer('See [[some/path|Custom Display]] here')
    console.log('gfm only raw:', JSON.stringify(tokens[0]?.raw))
    ed.destroy()
  })

  it('gfm:false, breaks:true', () => {
    const ed = makeEditor({ gfm: false, breaks: true })
    const tokens = ed.storage.markdown.manager.instance.lexer('See [[some/path|Custom Display]] here')
    console.log('breaks only raw:', JSON.stringify(tokens[0]?.raw))
    ed.destroy()
  })

  it('gfm:false, breaks:false', () => {
    const ed = makeEditor({ gfm: false, breaks: false })
    const tokens = ed.storage.markdown.manager.instance.lexer('See [[some/path|Custom Display]] here')
    console.log('neither raw:', JSON.stringify(tokens[0]?.raw))
    ed.destroy()
  })

  it('checks inline tokens with gfm+breaks', () => {
    const ed = makeEditor({ gfm: true, breaks: true })
    const tokens = ed.storage.markdown.manager.instance.lexer('See [[some/path|Custom Display]] here')
    console.log('inline tokens:', JSON.stringify(tokens[0]?.tokens, null, 2))
    ed.destroy()
  })
})
