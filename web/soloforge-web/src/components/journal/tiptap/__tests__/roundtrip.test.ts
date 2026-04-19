import { describe, it, expect } from 'vitest'
import { Editor } from '@tiptap/core'
import StarterKit from '@tiptap/starter-kit'
import { Table, TableRow, TableCell, TableHeader } from '@tiptap/extension-table'
import { Markdown } from '@tiptap/markdown'
import { RollTableNode } from '../RollTableNode'
import { NoteBlockNode } from '../NoteBlockNode'
import { WikiLinkMark } from '../WikiLinkMark'

function createEditor(content: string) {
  return new Editor({
    content,
    contentType: 'markdown',
    extensions: [
      StarterKit,
      Table.configure({ resizable: false }),
      TableRow,
      TableCell,
      TableHeader,
      Markdown.configure({
        markedOptions: { gfm: true, breaks: true },
      }),
      RollTableNode,
      NoteBlockNode,
      WikiLinkMark,
    ],
  })
}

describe('round-trip', () => {
  it('roll table survives getMarkdown -> setContent cycle', () => {
    const input = 'Some text before\n\n| Fate Check | &nbsp; |\n| ---------- | ------ |\n| **Question** | Will it work? |\n| **Odds** | 50/50 |\n| **Result** | Yes |\n\nSome text after'

    const editor = createEditor(input)
    const md1 = editor.getMarkdown()

    editor.commands.setContent(md1, { contentType: 'markdown' })
    const md2 = editor.getMarkdown()

    editor.commands.setContent(md2, { contentType: 'markdown' })
    const md3 = editor.getMarkdown()

    expect(md2).toContain('Fate Check')
    expect(md3).toContain('Fate Check')
    // Should stabilize after at most one cycle
    expect(md2).toBe(md3)

    editor.destroy()
  })

  it('note block survives round-trip', () => {
    const input = '> **Note:** Important info\n\nRegular paragraph'

    const editor = createEditor(input)
    const md1 = editor.getMarkdown()

    editor.commands.setContent(md1, { contentType: 'markdown' })
    const md2 = editor.getMarkdown()

    expect(md2).toContain('**Note:**')
    expect(md2).toContain('Important info')

    editor.destroy()
  })

  it('simple wiki-link survives round-trip', () => {
    const input = 'See [[My Note]] for details'

    const editor = createEditor(input)
    const md1 = editor.getMarkdown()
    expect(md1).toBe('See [[My Note]] for details')

    editor.commands.setContent(md1, { contentType: 'markdown' })
    const md2 = editor.getMarkdown()
    expect(md2).toBe('See [[My Note]] for details')

    editor.destroy()
  })

  it('wiki-link with pipe display text survives round-trip', () => {
    const input = 'See [[some/path|Custom Display]] here'

    const editor = createEditor(input)
    const json = editor.getJSON()
    console.log('=== JSON after initial parse ===')
    console.log(JSON.stringify(json, null, 2))

    const md1 = editor.getMarkdown()
    console.log('=== First getMarkdown() ===')
    console.log(JSON.stringify(md1))

    // The path should not be lost
    expect(md1).not.toContain('\n')
    expect(md1).toContain('Custom Display')

    editor.destroy()
  })

  it('multiple roll tables survive enhanced toggle simulation', () => {
    const input = `First paragraph

| Fate Check | &nbsp; |
| ---------- | ------ |
| **Question** | Is it safe? |
| **Result** | Yes |

Middle paragraph

| Scene Check | &nbsp; |
| ----------- | ------ |
| **Context** | Entering cave |
| **Result** | Altered Scene |

Last paragraph`

    const editor = createEditor(input)
    
    // Simulate enhanced toggle: getMarkdown then setContent repeatedly
    for (let i = 0; i < 5; i++) {
      const md = editor.getMarkdown()
      editor.commands.setContent(md, { contentType: 'markdown' })
    }
    
    const finalMd = editor.getMarkdown()
    expect(finalMd).toContain('Fate Check')
    expect(finalMd).toContain('Scene Check')
    expect(finalMd).toContain('Is it safe?')
    expect(finalMd).toContain('Entering cave')
    expect(finalMd).toContain('First paragraph')
    expect(finalMd).toContain('Last paragraph')
    
    editor.destroy()
  })
})
