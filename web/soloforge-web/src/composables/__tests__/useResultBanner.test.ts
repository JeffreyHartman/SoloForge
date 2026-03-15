import { describe, it, expect } from 'vitest'
import {
  formatFateResult,
  formatSceneResult,
  formatRandomResult,
  formatMeaningResult,
  formatQuickSetResult,
  formatDiceResult,
} from '../useResultBanner'
import type {
  FateCheckResponse,
  SceneCheckResponse,
  RandomEventResult,
  MeaningResult,
  QuickSetResult,
  DiceRollResponse,
} from '../../types'

describe('formatFateResult', () => {
  it('formats basic fate check', () => {
    const input: FateCheckResponse = {
      chaos: 5,
      odds: 'Likely',
      fate: { roll: 42, result: 'Yes', randomEventTriggered: false },
    }
    const result = formatFateResult(input)
    expect(result.type).toBe('fate')
    expect(result.title).toBe('Yes')
    expect(result.detail).toContain('Roll 42')
    expect(result.detail).toContain('Odds Likely')
    expect(result.detail).toContain('Chaos 5')
    expect(result.subDetail).toBeUndefined()
  })

  it('includes random event in subDetail', () => {
    const input: FateCheckResponse = {
      chaos: 7,
      odds: 'FiftyFifty',
      fate: { roll: 11, result: 'Exceptional Yes', randomEventTriggered: true },
      randomEvent: {
        eventFocus: 'NPC Action',
        eventAction: 'Betray',
        selectedCharacter: null,
        selectedThread: null,
        isNewNpc: false,
        listWasEmpty: false,
      },
    }
    const result = formatFateResult(input)
    expect(result.subDetail).toContain('NPC Action')
    expect(result.subDetail).toContain('Betray')
  })
})

describe('formatSceneResult', () => {
  it('formats basic scene check', () => {
    const input: SceneCheckResponse = {
      chaos: 4,
      scene: { roll: 7, result: 'Normal', sceneAdjustment: null, randomEvent: null },
    }
    const result = formatSceneResult(input)
    expect(result.type).toBe('scene')
    expect(result.title).toBe('Normal')
    expect(result.detail).toContain('Roll 7')
    expect(result.subDetail).toBeUndefined()
  })

  it('includes adjustment and random event', () => {
    const input: SceneCheckResponse = {
      chaos: 6,
      scene: {
        roll: 3,
        result: 'Altered',
        sceneAdjustment: 'Remove character',
        randomEvent: {
          eventFocus: 'Ambiguous',
          eventAction: 'Move Away',
          selectedCharacter: null,
          selectedThread: null,
          isNewNpc: false,
          listWasEmpty: false,
        },
      },
    }
    const result = formatSceneResult(input)
    expect(result.subDetail).toContain('Adjustment: Remove character')
    expect(result.subDetail).toContain('Ambiguous')
  })
})

describe('formatRandomResult', () => {
  it('formats basic event', () => {
    const input: RandomEventResult = {
      eventFocus: 'Remote Event',
      eventAction: 'Discover',
      selectedCharacter: null,
      selectedThread: null,
      isNewNpc: false,
      listWasEmpty: false,
    }
    const result = formatRandomResult(input)
    expect(result.type).toBe('event')
    expect(result.title).toBe('Remote Event')
    expect(result.detail).toBe('Discover')
    expect(result.subDetail).toBeUndefined()
  })

  it('includes character and thread', () => {
    const input: RandomEventResult = {
      eventFocus: 'NPC Action',
      eventAction: 'Help',
      selectedCharacter: 'Aldric',
      selectedThread: 'Quest',
      isNewNpc: false,
      listWasEmpty: false,
    }
    const result = formatRandomResult(input)
    expect(result.subDetail).toContain('Character: Aldric')
    expect(result.subDetail).toContain('Thread: Quest')
  })
})

describe('formatMeaningResult', () => {
  it('formats meaning with meta', () => {
    const input: MeaningResult = {
      tableName: 'Action',
      word1: 'Destroy',
      word2: 'Hope',
      isFusion: false,
      combined: 'Destroy Hope',
    }
    const result = formatMeaningResult(input, 'Action Table')
    expect(result.type).toBe('meaning')
    expect(result.title).toBe('Destroy Hope')
    expect(result.detail).toContain('Action Table')
    expect(result.detail).toContain('Destroy + Hope')
  })

  it('omits meta when null', () => {
    const input: MeaningResult = {
      tableName: 'Description',
      word1: 'Old',
      word2: 'Castle',
      isFusion: false,
      combined: 'Old Castle',
    }
    const result = formatMeaningResult(input, null)
    expect(result.detail).toBe('Old + Castle')
  })
})

describe('formatQuickSetResult', () => {
  it('formats quick set with multiple results', () => {
    const input: QuickSetResult = {
      quickSet: { id: 'qs1', name: 'Fantasy NPC', description: '', steps: [] },
      results: [
        { label: 'Trait', words: ['Bold'], combined: 'Bold', tableId: 't1' },
        { label: 'Goal', words: ['Power'], combined: 'Power', tableId: 't2' },
      ],
    }
    const result = formatQuickSetResult(input)
    expect(result.type).toBe('meaning')
    expect(result.title).toBe('Fantasy NPC')
    expect(result.detail).toContain('Trait: Bold')
    expect(result.detail).toContain('Goal: Power')
  })
})

describe('formatDiceResult', () => {
  it('formats dice roll with breakdown', () => {
    const input: DiceRollResponse = {
      roll: { summary: '2d6+3', total: 11, diceTotal: 8, modifier: 3, terms: [] },
      breakdown: '[4, 4] + 3',
    }
    const result = formatDiceResult(input)
    expect(result.type).toBe('dice')
    expect(result.title).toBe('2d6+3 = 11')
    expect(result.detail).toBe('[4, 4] + 3')
  })

  it('omits detail when breakdown is empty', () => {
    const input: DiceRollResponse = {
      roll: { summary: 'd20', total: 15, diceTotal: 15, modifier: 0, terms: [] },
      breakdown: '',
    }
    const result = formatDiceResult(input)
    expect(result.detail).toBeUndefined()
  })
})
