import { describe, it, expect } from 'vitest'
import { isPureAppend, isNearBottom } from '../editorState'

describe('isPureAppend', () => {
  it('returns true when old is empty and new has content', () => {
    expect(isPureAppend('', 'hello')).toBe(true)
  })

  it('returns false when old equals new', () => {
    expect(isPureAppend('hello', 'hello')).toBe(false)
  })

  it('returns true when new starts with old and is longer', () => {
    expect(isPureAppend('hello', 'hello world')).toBe(true)
  })

  it('returns false when new does not start with old', () => {
    expect(isPureAppend('hello', 'goodbye world')).toBe(false)
  })

  it('returns false when new is shorter than old', () => {
    expect(isPureAppend('hello world', 'hello')).toBe(false)
  })

  it('returns false when new is empty and old is not', () => {
    expect(isPureAppend('hello', '')).toBe(false)
  })

  it('returns false when both are empty', () => {
    expect(isPureAppend('', '')).toBe(false)
  })
})

describe('isNearBottom', () => {
  const threshold = 80

  it('returns true at exact bottom (remaining distance 0)', () => {
    // scrollHeight 1000, clientHeight 80, scrolled to 920 → 0 remaining
    expect(isNearBottom(920, 1000, 80, threshold)).toBe(true)
  })

  it('returns true within threshold', () => {
    // 20 remaining, threshold 80
    expect(isNearBottom(900, 1000, 80, threshold)).toBe(true)
  })

  it('returns true right at threshold boundary (exclusive upper)', () => {
    // 79 remaining, threshold 80 → still true
    expect(isNearBottom(841, 1000, 80, threshold)).toBe(true)
  })

  it('returns false beyond threshold', () => {
    // 820 remaining, threshold 80 → false
    expect(isNearBottom(100, 1000, 80, threshold)).toBe(false)
  })

  it('returns false for NaN scrollTop', () => {
    expect(isNearBottom(NaN, 1000, 80, threshold)).toBe(false)
  })

  it('returns false for NaN scrollHeight', () => {
    expect(isNearBottom(100, NaN, 80, threshold)).toBe(false)
  })

  it('returns false for NaN clientHeight', () => {
    expect(isNearBottom(100, 1000, NaN, threshold)).toBe(false)
  })
})
