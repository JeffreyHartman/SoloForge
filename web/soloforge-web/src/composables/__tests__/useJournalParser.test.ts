import { describe, it, expect } from 'vitest'
import { ref } from 'vue'
import {
  hashId,
  parseTableFields,
  parseSegments,
  useJournalParser,
} from '../useJournalParser'

describe('hashId', () => {
  it('returns a deterministic string hash', () => {
    const a = hashId('some raw text', 0)
    const b = hashId('some raw text', 0)
    expect(a).toBe(b)
  })

  it('returns different hashes for different inputs', () => {
    expect(hashId('text a', 0)).not.toBe(hashId('text b', 0))
  })

  it('returns different hashes for same text but different index', () => {
    expect(hashId('text', 0)).not.toBe(hashId('text', 1))
  })

  it('starts with "s" prefix', () => {
    expect(hashId('anything', 0)).toMatch(/^s/)
  })
})

describe('parseTableFields', () => {
  it('parses a Fate Check table', () => {
    const table = [
      '| Fate Check | &nbsp; |',
      '| ---------- | ------ |',
      '| **Question** | Likely |',
      '| **Result** | Yes |',
      '| *Details* | Roll: 42 |',
    ].join('\n')

    const result = parseTableFields(table)
    expect(result).not.toBeNull()
    expect(result!.rollType).toBe('Fate Check')
    expect(result!.fields['Question']).toBe('Likely')
    expect(result!.fields['Result']).toBe('Yes')
    expect(result!.fields['Details']).toBe('Roll: 42')
  })

  it('returns null for unrecognized roll types', () => {
    const table = [
      '| Custom Table | &nbsp; |',
      '| ------------ | ------ |',
      '| **Field** | Value |',
    ].join('\n')
    expect(parseTableFields(table)).toBeNull()
  })

  it('returns null for tables with fewer than 3 lines', () => {
    expect(parseTableFields('| Header |\n| --- |')).toBeNull()
  })

  it('parses all known roll types', () => {
    for (const type of ['Fate Check', 'Scene Check', 'Random Event', 'Meaning Roll', 'Dice Roll']) {
      const table = `| ${type} | &nbsp; |\n| --- | --- |\n| **Field** | Value |`
      const result = parseTableFields(table)
      expect(result).not.toBeNull()
      expect(result!.rollType).toBe(type)
    }
  })
})

describe('parseSegments', () => {
  it('returns empty array for blank text', () => {
    expect(parseSegments('')).toEqual([])
    expect(parseSegments('   ')).toEqual([])
  })

  it('parses plain text as a single text segment', () => {
    const segments = parseSegments('Hello world')
    expect(segments).toHaveLength(1)
    expect(segments[0].type).toBe('text')
    expect(segments[0].raw).toBe('Hello world')
  })

  it('parses a roll table into a roll segment', () => {
    const text = [
      '| Fate Check | &nbsp; |',
      '| ---------- | ------ |',
      '| **Result** | Yes |',
    ].join('\n')

    const segments = parseSegments(text)
    expect(segments).toHaveLength(1)
    expect(segments[0].type).toBe('roll')
    if (segments[0].type === 'roll') {
      expect(segments[0].rollType).toBe('Fate Check')
      expect(segments[0].fields['Result']).toBe('Yes')
    }
  })

  it('parses note blockquotes as roll segments with Note type', () => {
    const text = '> **Note:** This is important'
    const segments = parseSegments(text)
    expect(segments).toHaveLength(1)
    expect(segments[0].type).toBe('roll')
    if (segments[0].type === 'roll') {
      expect(segments[0].rollType).toBe('Note')
      expect(segments[0].fields['Note']).toBe('This is important')
    }
  })

  it('parses mixed content with text and roll tables', () => {
    const text = [
      'Some narrative text.',
      '',
      '| Fate Check | &nbsp; |',
      '| ---------- | ------ |',
      '| **Result** | No |',
      '',
      'More text after the roll.',
    ].join('\n')

    const segments = parseSegments(text)
    expect(segments).toHaveLength(3)
    expect(segments[0].type).toBe('text')
    expect(segments[1].type).toBe('roll')
    expect(segments[2].type).toBe('text')
  })

  it('tracks offsets correctly', () => {
    const text = 'Line one\n\n| Fate Check | &nbsp; |\n| ---------- | ------ |\n| **Result** | Yes |'
    const segments = parseSegments(text)
    for (const seg of segments) {
      expect(text.slice(seg.offset, seg.offset + seg.raw.length)).toBe(seg.raw)
    }
  })

  it('treats unrecognized tables as text', () => {
    const text = [
      '| Custom | Table |',
      '| ------ | ----- |',
      '| Data   | Here  |',
    ].join('\n')
    const segments = parseSegments(text)
    expect(segments).toHaveLength(1)
    expect(segments[0].type).toBe('text')
  })
})

describe('useJournalParser - deleteSegment', () => {
  it('removes a roll segment and joins surrounding text', () => {
    const content = ref(
      'Before text.\n\n' +
      '| Fate Check | &nbsp; |\n| ---------- | ------ |\n| **Result** | No |\n\n' +
      'After text.',
    )
    const { segments, deleteSegment } = useJournalParser(content)
    const roll = segments.value.find(s => s.type === 'roll')
    expect(roll).toBeDefined()
    deleteSegment(roll!.id)
    expect(content.value).toContain('Before text.')
    expect(content.value).toContain('After text.')
    expect(content.value).not.toContain('Fate Check')
  })

  it('handles deleting when segment is at the start', () => {
    const content = ref(
      '| Fate Check | &nbsp; |\n| ---------- | ------ |\n| **Result** | Yes |\n\nSome text.',
    )
    const { segments, deleteSegment } = useJournalParser(content)
    const roll = segments.value.find(s => s.type === 'roll')
    deleteSegment(roll!.id)
    expect(content.value).toBe('Some text.')
  })

  it('does nothing for unknown segment ID', () => {
    const content = ref('Some text.')
    const { deleteSegment } = useJournalParser(content)
    deleteSegment('nonexistent')
    expect(content.value).toBe('Some text.')
  })
})
