import { describe, it, expect } from 'vitest'
import {
  fateCheckToMarkdown,
  sceneCheckToMarkdown,
  randomEventToMarkdown,
  meaningToMarkdown,
  quickSetToMarkdown,
  diceRollToMarkdown,
} from '../useRollMarkdown'
import type {
  FateCheckResponse,
  SceneCheckResponse,
  RandomEventResult,
  MeaningResult,
  QuickSetResult,
  DiceRollResponse,
} from '../../types'

describe('fateCheckToMarkdown', () => {
  it('formats a fate check with all fields', () => {
    const input: FateCheckResponse = {
      chaos: 5,
      odds: 'Likely',
      fate: { roll: 42, result: 'Yes', randomEventTriggered: false },
    }
    const md = fateCheckToMarkdown(input)
    expect(md).toContain('| Fate Check |')
    expect(md).toContain('| **Question** | Likely |')
    expect(md).toContain('| **Result** | Yes |')
    expect(md).toContain('Roll: 42')
    expect(md).toContain('Chaos: 5')
  })

  it('handles extreme fate result', () => {
    const input: FateCheckResponse = {
      chaos: 9,
      odds: 'Impossible',
      fate: { roll: 1, result: 'Exceptional Yes', randomEventTriggered: true },
      randomEvent: { eventFocus: 'NPC Action', eventAction: 'Betray', selectedCharacter: null, selectedThread: null, isNewNpc: false, listWasEmpty: false },
    }
    const md = fateCheckToMarkdown(input)
    expect(md).toContain('Exceptional Yes')
    expect(md).toContain('Impossible')
  })
})

describe('sceneCheckToMarkdown', () => {
  it('formats a basic scene check', () => {
    const input: SceneCheckResponse = {
      chaos: 4,
      scene: { roll: 7, result: 'Normal', sceneAdjustment: null, randomEvent: null },
    }
    const md = sceneCheckToMarkdown(input)
    expect(md).toContain('| Scene Check |')
    expect(md).toContain('| **Result** | Normal |')
    expect(md).toContain('Roll: 7')
    expect(md).toContain('Chaos: 4')
    expect(md).not.toContain('Adjustment')
  })

  it('includes scene adjustment when present', () => {
    const input: SceneCheckResponse = {
      chaos: 6,
      scene: { roll: 3, result: 'Altered', sceneAdjustment: 'Add element', randomEvent: null },
    }
    const md = sceneCheckToMarkdown(input)
    expect(md).toContain('Adjustment: Add element')
  })
})

describe('randomEventToMarkdown', () => {
  it('formats a random event with no character or thread', () => {
    const input: RandomEventResult = {
      eventFocus: 'Remote Event',
      eventAction: 'Discover Truth',
      selectedCharacter: null,
      selectedThread: null,
      isNewNpc: false,
      listWasEmpty: false,
    }
    const md = randomEventToMarkdown(input)
    expect(md).toContain('| Random Event |')
    expect(md).toContain('Remote Event: Discover Truth')
    expect(md).not.toContain('Details')
  })

  it('includes character and thread when present', () => {
    const input: RandomEventResult = {
      eventFocus: 'NPC Action',
      eventAction: 'Attack',
      selectedCharacter: 'Aldric',
      selectedThread: 'Find the artifact',
      isNewNpc: false,
      listWasEmpty: false,
    }
    const md = randomEventToMarkdown(input)
    expect(md).toContain('Character: Aldric')
    expect(md).toContain('Thread: Find the artifact')
  })
})

describe('meaningToMarkdown', () => {
  it('formats a meaning roll', () => {
    const input: MeaningResult = {
      tableName: 'Action',
      word1: 'Destroy',
      word2: 'Hope',
      isFusion: false,
      combined: 'Destroy Hope',
    }
    const md = meaningToMarkdown(input)
    expect(md).toContain('| Meaning Roll |')
    expect(md).toContain('| **Result** | Destroy Hope |')
    expect(md).toContain('Table: Action')
  })
})

describe('quickSetToMarkdown', () => {
  it('formats a quick set result with multiple entries', () => {
    const input: QuickSetResult = {
      quickSet: { id: 'qs1', name: 'Fantasy NPC', description: '', steps: [] },
      results: [
        { label: 'Trait', words: ['Bold'], combined: 'Bold', tableId: 't1' },
        { label: 'Motivation', words: ['Power'], combined: 'Power', tableId: 't2' },
      ],
    }
    const md = quickSetToMarkdown(input)
    expect(md).toContain('| Quick Set |')
    expect(md).toContain('Fantasy NPC Generated')
    expect(md).toContain('Trait: Bold')
    expect(md).toContain('Motivation: Power')
  })
})

describe('diceRollToMarkdown', () => {
  it('formats a dice roll with breakdown', () => {
    const input: DiceRollResponse = {
      roll: { summary: '2d6+3', total: 11, diceTotal: 8, modifier: 3, terms: [] },
      breakdown: '[4, 4] + 3',
    }
    const md = diceRollToMarkdown(input)
    expect(md).toContain('| Dice Roll |')
    expect(md).toContain('| **Expression** | 2d6+3 |')
    expect(md).toContain('| **Total** | 11 |')
    expect(md).toContain('[4, 4] + 3')
  })

  it('omits details row when no breakdown', () => {
    const input: DiceRollResponse = {
      roll: { summary: 'd20', total: 15, diceTotal: 15, modifier: 0, terms: [] },
      breakdown: '',
    }
    const md = diceRollToMarkdown(input)
    expect(md).not.toContain('Details')
  })
})
