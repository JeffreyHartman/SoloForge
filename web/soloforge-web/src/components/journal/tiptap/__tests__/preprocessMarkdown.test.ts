import { describe, it, expect } from 'vitest'
import { encodeAttr, parseTableFields, preprocessForWysiwyg } from '../preprocessMarkdown'

describe('encodeAttr', () => {
  it('encodes ampersands', () => {
    expect(encodeAttr('a & b')).toBe('a &amp; b')
  })

  it('encodes double quotes', () => {
    expect(encodeAttr('say "hello"')).toBe('say &quot;hello&quot;')
  })

  it('encodes angle brackets', () => {
    expect(encodeAttr('<div>')).toBe('&lt;div&gt;')
  })

  it('encodes newlines', () => {
    expect(encodeAttr('line1\nline2')).toBe('line1&#10;line2')
  })

  it('handles multiple entities in one string', () => {
    expect(encodeAttr('a & "b" <c>\nd')).toBe('a &amp; &quot;b&quot; &lt;c&gt;&#10;d')
  })
})

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
})

describe('preprocessForWysiwyg', () => {
  it('returns empty string for empty input', () => {
    expect(preprocessForWysiwyg('')).toBe('')
  })

  it('passes through plain text unchanged', () => {
    expect(preprocessForWysiwyg('Hello world')).toBe('Hello world')
  })

  it('preserves extra blank lines with <p></p> tags', () => {
    const input = 'A\n\n\nB'
    const result = preprocessForWysiwyg(input)
    expect(result).toContain('<p></p>')
    expect(result).toContain('A')
    expect(result).toContain('B')
  })

  it('converts note blockquotes to HTML divs', () => {
    const input = '> **Note:** Important info'
    const result = preprocessForWysiwyg(input)
    expect(result).toContain('data-note-block="true"')
    expect(result).toContain('Important info')
  })

  it('converts roll tables to HTML divs', () => {
    const input = [
      '| Fate Check | &nbsp; |',
      '| ---------- | ------ |',
      '| **Result** | Yes |',
    ].join('\n')
    const result = preprocessForWysiwyg(input)
    expect(result).toContain('data-roll-table="true"')
    expect(result).toContain('data-roll-type="Fate Check"')
  })

  it('converts wiki-links to HTML spans', () => {
    const input = 'See [[My Note]] for details'
    const result = preprocessForWysiwyg(input)
    expect(result).toContain('data-wiki-link="true"')
    expect(result).toContain('data-path="My Note.md"')
    expect(result).toContain('My Note')
  })

  it('handles wiki-links with display text', () => {
    const input = '[[path/to/note|Display Name]]'
    const result = preprocessForWysiwyg(input)
    expect(result).toContain('data-path="path/to/note.md"')
    expect(result).toContain('Display Name')
  })

  it('does not add .md if path already ends with .md', () => {
    const input = '[[my-note.md]]'
    const result = preprocessForWysiwyg(input)
    expect(result).toContain('data-path="my-note.md"')
    // Should not have double .md
    expect(result).not.toContain('my-note.md.md')
  })

  it('keeps unrecognized tables as plain markdown', () => {
    const input = [
      '| Custom | Table |',
      '| ------ | ----- |',
      '| Data   | Here  |',
    ].join('\n')
    const result = preprocessForWysiwyg(input)
    expect(result).not.toContain('data-roll-table')
    expect(result).toContain('| Custom | Table |')
  })

  it('skips roll table conversion when enhanced is false', () => {
    const input = [
      '| Fate Check | &nbsp; |',
      '| ---------- | ------ |',
      '| **Result** | Yes |',
    ].join('\n')
    const result = preprocessForWysiwyg(input, { enhanced: false })
    expect(result).not.toContain('data-roll-table')
    expect(result).toContain('| Fate Check | &nbsp; |')
    expect(result).toContain('| **Result** | Yes |')
  })

  it('converts roll tables when enhanced is true', () => {
    const input = [
      '| Fate Check | &nbsp; |',
      '| ---------- | ------ |',
      '| **Result** | Yes |',
    ].join('\n')
    const result = preprocessForWysiwyg(input, { enhanced: true })
    expect(result).toContain('data-roll-table="true"')
  })

  it('still converts note blocks when enhanced is false', () => {
    const input = '> **Note:** Important info'
    const result = preprocessForWysiwyg(input, { enhanced: false })
    expect(result).toContain('data-note-block="true"')
  })

  it('still converts wiki-links when enhanced is false', () => {
    const input = 'See [[My Note]] for details'
    const result = preprocessForWysiwyg(input, { enhanced: false })
    expect(result).toContain('data-wiki-link="true"')
  })
})
