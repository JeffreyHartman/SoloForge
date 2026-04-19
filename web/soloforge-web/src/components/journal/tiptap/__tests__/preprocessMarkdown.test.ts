import { describe, it, expect } from 'vitest'
import { parseTableFields, RollTableNode } from '../RollTableNode'
import { NoteBlockNode } from '../NoteBlockNode'
import { WikiLinkMark } from '../WikiLinkMark'

describe('parseTableFields', () => {
  it('parses a valid Fate Check table', () => {
    const table = [
      '| Fate Check | &nbsp; |',
      '| ---------- | ------ |',
      '| **Result** | Yes |',
    ].join('\n')
    const result = parseTableFields(table)
    expect(result).not.toBeNull()
    expect(result!.rollType).toBe('Fate Check')
    expect(result!.fields['Result']).toBe('Yes')
  })

  it('returns null for unknown roll type', () => {
    const table = '| Unknown | &nbsp; |\n| --- | --- |\n| **X** | Y |'
    expect(parseTableFields(table)).toBeNull()
  })

  it('returns null for too few lines', () => {
    expect(parseTableFields('| Fate Check |')).toBeNull()
  })

  it('parses all known roll types', () => {
    for (const type of ['Fate Check', 'Scene Check', 'Random Event', 'Meaning Roll', 'Dice Roll']) {
      const table = `| ${type} | &nbsp; |\n| --- | --- |\n| **Result** | Value |`
      const result = parseTableFields(table)
      expect(result).not.toBeNull()
      expect(result!.rollType).toBe(type)
    }
  })

  it('parses multiple fields', () => {
    const table = [
      '| Fate Check | &nbsp; |',
      '| ---------- | ------ |',
      '| **Question** | Will it work? |',
      '| **Odds** | 50/50 |',
      '| **Result** | Yes |',
    ].join('\n')
    const result = parseTableFields(table)
    expect(result).not.toBeNull()
    expect(result!.fields['Question']).toBe('Will it work?')
    expect(result!.fields['Odds']).toBe('50/50')
    expect(result!.fields['Result']).toBe('Yes')
  })
})

describe('RollTableNode tokenizer', () => {
  const tokenizer = RollTableNode.config.markdownTokenizer!

  it('tokenizes a recognized roll table', () => {
    const src = '| Fate Check | &nbsp; |\n| ---------- | ------ |\n| **Result** | Yes |\n'
    const result = tokenizer.tokenize(src, [], {} as any)
    expect(result).toBeDefined()
    expect(result!.type).toBe('rollTable')
    expect(result!.rollType).toBe('Fate Check')
  })

  it('returns undefined for unrecognized tables', () => {
    const src = '| Custom | Table |\n| ------ | ----- |\n| Data   | Here  |\n'
    const result = tokenizer.tokenize(src, [], {} as any)
    expect(result).toBeUndefined()
  })

  it('returns undefined for non-table content', () => {
    const result = tokenizer.tokenize('Hello world', [], {} as any)
    expect(result).toBeUndefined()
  })
})

describe('NoteBlockNode tokenizer', () => {
  const tokenizer = NoteBlockNode.config.markdownTokenizer!

  it('tokenizes a single-line note', () => {
    const src = '> **Note:** Important info\n'
    const result = tokenizer.tokenize(src, [], {} as any)
    expect(result).toBeDefined()
    expect(result!.type).toBe('noteBlock')
    expect(result!.noteText).toBe('Important info')
  })

  it('tokenizes a multi-line note', () => {
    const src = '> **Note:** First line\n> Second line\n> Third line\n'
    const result = tokenizer.tokenize(src, [], {} as any)
    expect(result).toBeDefined()
    expect(result!.type).toBe('noteBlock')
    expect(result!.noteText).toBe('First line\nSecond line\nThird line')
  })

  it('stops at non-continuation lines', () => {
    const src = '> **Note:** A note\n\nRegular text'
    const result = tokenizer.tokenize(src, [], {} as any)
    expect(result).toBeDefined()
    expect(result!.noteText).toBe('A note')
    expect(result!.raw).not.toContain('Regular text')
  })

  it('returns undefined for regular blockquotes', () => {
    const result = tokenizer.tokenize('> Just a quote\n', [], {} as any)
    expect(result).toBeUndefined()
  })
})

describe('WikiLinkMark tokenizer', () => {
  const tokenizer = WikiLinkMark.config.markdownTokenizer!

  it('tokenizes a simple wiki-link', () => {
    const src = '[[My Note]]'
    const result = tokenizer.tokenize(src, [], {} as any)
    expect(result).toBeDefined()
    expect(result!.type).toBe('wikiLink')
    expect(result!.path).toBe('My Note.md')
    expect(result!.display).toBe('My Note')
  })

  it('tokenizes a wiki-link with display text', () => {
    const src = '[[path/to/note|Display Name]]'
    const result = tokenizer.tokenize(src, [], {} as any)
    expect(result).toBeDefined()
    expect(result!.path).toBe('path/to/note.md')
    expect(result!.display).toBe('Display Name')
  })

  it('does not double-add .md extension', () => {
    const src = '[[my-note.md]]'
    const result = tokenizer.tokenize(src, [], {} as any)
    expect(result).toBeDefined()
    expect(result!.path).toBe('my-note.md')
  })

  it('returns undefined for non-wiki-link content', () => {
    const result = tokenizer.tokenize('Hello world', [], {} as any)
    expect(result).toBeUndefined()
  })

  it('returns undefined for incomplete brackets', () => {
    const result = tokenizer.tokenize('[[incomplete', [], {} as any)
    expect(result).toBeUndefined()
  })
})
