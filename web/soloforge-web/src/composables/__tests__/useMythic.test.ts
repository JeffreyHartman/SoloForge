import { describe, it, expect, vi, beforeEach } from 'vitest'
import type { MeaningResult, MeaningTableResponse, MeaningFusionResponse, QuickSetResult } from '../../types'

vi.mock('../useApi', () => ({
  apiSend: vi.fn(),
}))

import { apiSend } from '../useApi'
import { useMythic } from '../useMythic'

describe('useMythic', () => {
  beforeEach(() => {
    vi.mocked(apiSend).mockReset()
    const { clearResults, meaningMode, meaningTableId, meaningFusionTable1, meaningFusionTable2, meaningQuickSetId, meaningContext, fateQuestion, sceneContext, diceExpression } = useMythic()
    clearResults()
    meaningMode.value = 'action'
    meaningTableId.value = ''
    meaningFusionTable1.value = ''
    meaningFusionTable2.value = ''
    meaningQuickSetId.value = ''
    meaningContext.value = ''
    fateQuestion.value = ''
    sceneContext.value = ''
    diceExpression.value = ''
  })

  describe('runFateCheck', () => {
    it('trims question and sends null for empty', async () => {
      vi.mocked(apiSend).mockResolvedValue({ chaos: 5, odds: 'Likely', fate: { roll: 42, result: 'Yes', randomEventTriggered: false } })
      const { fateQuestion, runFateCheck } = useMythic()
      fateQuestion.value = '   '
      await runFateCheck()
      expect(apiSend).toHaveBeenCalledWith('/api/fate-check', 'POST', { odds: 'FiftyFifty', question: null })
    })

    it('sends trimmed question when present', async () => {
      vi.mocked(apiSend).mockResolvedValue({ chaos: 5, odds: 'Likely', fate: { roll: 42, result: 'Yes', randomEventTriggered: false } })
      const { fateQuestion, runFateCheck } = useMythic()
      fateQuestion.value = '  Is it safe?  '
      await runFateCheck()
      expect(apiSend).toHaveBeenCalledWith('/api/fate-check', 'POST', expect.objectContaining({ question: 'Is it safe?' }))
    })
  })

  describe('runMeaning', () => {
    it('routes action mode to /api/meaning/action', async () => {
      const mockResult: MeaningResult = { tableName: 'Action', word1: 'A', word2: 'B', isFusion: false, combined: 'A B' }
      vi.mocked(apiSend).mockResolvedValue(mockResult)
      const { meaningMode, runMeaning, meaningResult } = useMythic()
      meaningMode.value = 'action'
      await runMeaning()
      expect(apiSend).toHaveBeenCalledWith('/api/meaning/action', 'POST', { context: null })
      expect(meaningResult.value).toEqual(mockResult)
    })

    it('routes description mode to /api/meaning/description', async () => {
      const mockResult: MeaningResult = { tableName: 'Desc', word1: 'X', word2: 'Y', isFusion: false, combined: 'X Y' }
      vi.mocked(apiSend).mockResolvedValue(mockResult)
      const { meaningMode, runMeaning } = useMythic()
      meaningMode.value = 'description'
      await runMeaning()
      expect(apiSend).toHaveBeenCalledWith('/api/meaning/description', 'POST', { context: null })
    })

    it('table mode throws when no table selected', async () => {
      const { meaningMode, meaningTableId, runMeaning } = useMythic()
      meaningMode.value = 'table'
      meaningTableId.value = ''
      await expect(runMeaning()).rejects.toThrow('Select a table first.')
    })

    it('table mode sends tableId and extracts result', async () => {
      const mockResp: MeaningTableResponse = {
        table: { id: 't1', displayName: 'Actions' },
        meaning: { tableName: 'Actions', word1: 'C', word2: 'D', isFusion: false, combined: 'C D' },
      }
      vi.mocked(apiSend).mockResolvedValue(mockResp)
      const { meaningMode, meaningTableId, runMeaning, meaningResult, meaningMeta } = useMythic()
      meaningMode.value = 'table'
      meaningTableId.value = 't1'
      await runMeaning()
      expect(meaningResult.value).toEqual(mockResp.meaning)
      expect(meaningMeta.value).toBe('Actions')
    })

    it('fusion mode throws when tables not selected', async () => {
      const { meaningMode, runMeaning } = useMythic()
      meaningMode.value = 'fusion'
      await expect(runMeaning()).rejects.toThrow('Select two tables first.')
    })

    it('fusion mode combines two table names in meta', async () => {
      const mockResp: MeaningFusionResponse = {
        table1: { id: 't1', displayName: 'Action' },
        table2: { id: 't2', displayName: 'Description' },
        meaning: { tableName: 'Fusion', word1: 'E', word2: 'F', isFusion: true, combined: 'E F' },
      }
      vi.mocked(apiSend).mockResolvedValue(mockResp)
      const { meaningMode, meaningFusionTable1, meaningFusionTable2, runMeaning, meaningMeta } = useMythic()
      meaningMode.value = 'fusion'
      meaningFusionTable1.value = 't1'
      meaningFusionTable2.value = 't2'
      await runMeaning()
      expect(meaningMeta.value).toBe('Action + Description')
    })

    it('quickSet mode throws when no set selected', async () => {
      const { meaningMode, runMeaning } = useMythic()
      meaningMode.value = 'quickSet'
      await expect(runMeaning()).rejects.toThrow('Select a quick set first.')
    })

    it('quickSet mode stores result and clears meaningResult', async () => {
      const mockQs: QuickSetResult = {
        quickSet: { id: 'qs1', name: 'NPC', description: '', steps: [] },
        results: [{ label: 'Trait', words: ['Bold'], combined: 'Bold', tableId: 't1' }],
      }
      vi.mocked(apiSend).mockResolvedValue(mockQs)
      const { meaningMode, meaningQuickSetId, runMeaning, quickSetResult, meaningResult } = useMythic()
      meaningMode.value = 'quickSet'
      meaningQuickSetId.value = 'qs1'
      await runMeaning()
      expect(quickSetResult.value).toEqual(mockQs)
      expect(meaningResult.value).toBeNull()
    })
  })

  describe('rollDice', () => {
    it('returns null for empty expression', async () => {
      const { rollDice } = useMythic()
      const result = await rollDice('')
      expect(result).toBeNull()
      expect(apiSend).not.toHaveBeenCalled()
    })

    it('clears expression after successful roll', async () => {
      vi.mocked(apiSend).mockResolvedValue({ roll: { summary: 'd20', total: 15, diceTotal: 15, modifier: 0, terms: [] }, breakdown: '' })
      const { diceExpression, rollDice } = useMythic()
      diceExpression.value = '2d6'
      await rollDice()
      expect(diceExpression.value).toBe('')
    })
  })

  describe('initMeaningDefaults', () => {
    it('sets defaults only when refs are empty', () => {
      const { meaningTableId, meaningFusionTable1, meaningFusionTable2, meaningQuickSetId, initMeaningDefaults } = useMythic()
      initMeaningDefaults('t1', ['f1', 'f2'], 'qs1')
      expect(meaningTableId.value).toBe('t1')
      expect(meaningFusionTable1.value).toBe('f1')
      expect(meaningFusionTable2.value).toBe('f2')
      expect(meaningQuickSetId.value).toBe('qs1')
    })

    it('does not overwrite existing values', () => {
      const { meaningTableId, meaningFusionTable1, meaningFusionTable2, meaningQuickSetId, initMeaningDefaults } = useMythic()
      meaningTableId.value = 'existing'
      meaningFusionTable1.value = 'existing1'
      meaningFusionTable2.value = 'existing2'
      meaningQuickSetId.value = 'existingQs'
      initMeaningDefaults('t1', ['f1', 'f2'], 'qs1')
      expect(meaningTableId.value).toBe('existing')
      expect(meaningFusionTable1.value).toBe('existing1')
      expect(meaningFusionTable2.value).toBe('existing2')
      expect(meaningQuickSetId.value).toBe('existingQs')
    })
  })

  describe('clearResults', () => {
    it('nullifies all result refs', () => {
      const { fateResult, sceneResult, randomResult, meaningResult, meaningMeta, quickSetResult, diceResult, clearResults } = useMythic()
      clearResults()
      expect(fateResult.value).toBeNull()
      expect(sceneResult.value).toBeNull()
      expect(randomResult.value).toBeNull()
      expect(meaningResult.value).toBeNull()
      expect(meaningMeta.value).toBeNull()
      expect(quickSetResult.value).toBeNull()
      expect(diceResult.value).toBeNull()
    })
  })
})
