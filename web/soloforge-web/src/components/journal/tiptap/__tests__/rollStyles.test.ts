import { describe, it, expect } from 'vitest'
import { getSummary, STYLES, DEFAULT_STYLE } from '../rollStyles'

describe('getSummary', () => {
  it('extracts Question and Result for Fate Check', () => {
    const result = getSummary('Fate Check', { Question: 'Will it work?', Result: 'Yes', Odds: '50/50' })
    expect(result).toEqual({ context: 'Will it work?', result: 'Yes' })
  })

  it('extracts Context and Result for Scene Check', () => {
    const result = getSummary('Scene Check', { Context: 'Entering the cave', Result: 'Altered Scene' })
    expect(result).toEqual({ context: 'Entering the cave', result: 'Altered Scene' })
  })

  it('extracts For and Result for Meaning Roll', () => {
    const result = getSummary('Meaning Roll', { For: 'NPC motivation', Result: 'Pursue wisdom' })
    expect(result).toEqual({ context: 'NPC motivation', result: 'Pursue wisdom' })
  })

  it('extracts Expression and Total for Dice Roll', () => {
    const result = getSummary('Dice Roll', { Expression: '2d6+3', Total: '11', Result: '11' })
    expect(result).toEqual({ context: '2d6+3', result: '11' })
  })

  it('falls back to Result for Dice Roll without Total', () => {
    const result = getSummary('Dice Roll', { Expression: '1d20', Result: '17' })
    expect(result).toEqual({ context: '1d20', result: '17' })
  })

  it('extracts Event for Random Event', () => {
    const result = getSummary('Random Event', { Event: 'NPC action', Result: 'Betray ally' })
    expect(result).toEqual({ context: '', result: 'NPC action' })
  })

  it('falls back to Result for Random Event without Event', () => {
    const result = getSummary('Random Event', { Result: 'Something happens' })
    expect(result).toEqual({ context: '', result: 'Something happens' })
  })

  it('returns first field value for unknown roll type', () => {
    const result = getSummary('Custom', { Foo: 'bar', Baz: 'qux' })
    expect(result).toEqual({ context: '', result: 'bar' })
  })

  it('handles empty fields gracefully', () => {
    const result = getSummary('Fate Check', {})
    expect(result).toEqual({ context: '', result: '' })
  })
})

describe('STYLES', () => {
  it('has entries for all known roll types', () => {
    expect(STYLES['Fate Check']).toBeDefined()
    expect(STYLES['Scene Check']).toBeDefined()
    expect(STYLES['Random Event']).toBeDefined()
    expect(STYLES['Meaning Roll']).toBeDefined()
    expect(STYLES['Dice Roll']).toBeDefined()
  })

  it('each style has required properties', () => {
    for (const style of Object.values(STYLES)) {
      expect(style).toHaveProperty('border')
      expect(style).toHaveProperty('color')
      expect(style).toHaveProperty('bg')
      expect(style).toHaveProperty('label')
    }
  })
})

describe('DEFAULT_STYLE', () => {
  it('has a label of Roll', () => {
    expect(DEFAULT_STYLE.label).toBe('Roll')
  })
})
