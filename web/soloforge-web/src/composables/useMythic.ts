import { ref, reactive } from 'vue'
import type {
  FateCheckResponse,
  SceneCheckResponse,
  RandomEventResult,
  MeaningResult,
  MeaningTableResponse,
  MeaningFusionResponse,
  QuickSetResult,
  DiceRollResponse,
  MeaningMode,
} from '../types'
import { apiSend } from './useApi'

// Fate Check
const fateOdds = ref<string>('FiftyFifty')
const fateQuestion = ref<string>('')
const fateResult = ref<FateCheckResponse | null>(null)

// Scene Check
const sceneContext = ref<string>('')
const sceneResult = ref<SceneCheckResponse | null>(null)

// Random Event
const randomResult = ref<RandomEventResult | null>(null)

// Meaning
const meaningMode = ref<MeaningMode>('action')
const meaningContext = ref<string>('')
const meaningTableId = ref<string>('')
const meaningFusionTable1 = ref<string>('')
const meaningFusionTable2 = ref<string>('')
const meaningQuickSetId = ref<string>('')
const meaningResult = ref<MeaningResult | null>(null)
const meaningMeta = ref<string | null>(null)
const quickSetResult = ref<QuickSetResult | null>(null)

// Dice
const diceExpression = ref<string>('')
const diceResult = ref<DiceRollResponse | null>(null)

const loading = reactive({
  fateCheck: false,
  sceneCheck: false,
  randomEvent: false,
  meaning: false,
  diceRoll: false,
})

export function useMythic() {
  async function runFateCheck(): Promise<FateCheckResponse> {
    loading.fateCheck = true
    try {
      fateResult.value = await apiSend<FateCheckResponse>('/api/fate-check', 'POST', {
        odds: fateOdds.value,
        question: fateQuestion.value.trim() || null,
      })
      return fateResult.value
    } finally {
      loading.fateCheck = false
    }
  }

  async function runSceneCheck(): Promise<SceneCheckResponse> {
    loading.sceneCheck = true
    try {
      sceneResult.value = await apiSend<SceneCheckResponse>('/api/scene-check', 'POST', {
        context: sceneContext.value.trim() || null,
      })
      return sceneResult.value
    } finally {
      loading.sceneCheck = false
    }
  }

  async function runRandomEvent(): Promise<RandomEventResult> {
    loading.randomEvent = true
    try {
      randomResult.value = await apiSend<RandomEventResult>('/api/random-event', 'POST')
      return randomResult.value
    } finally {
      loading.randomEvent = false
    }
  }

  async function runMeaning(): Promise<void> {
    loading.meaning = true
    meaningMeta.value = null
    try {
      const context = meaningContext.value.trim() || null

      if (meaningMode.value === 'action') {
        meaningResult.value = await apiSend<MeaningResult>('/api/meaning/action', 'POST', { context })
        quickSetResult.value = null
        return
      }

      if (meaningMode.value === 'description') {
        meaningResult.value = await apiSend<MeaningResult>('/api/meaning/description', 'POST', { context })
        quickSetResult.value = null
        return
      }

      if (meaningMode.value === 'table') {
        if (!meaningTableId.value) {
          throw new Error('Select a table first.')
        }
        const resp = await apiSend<MeaningTableResponse>('/api/meaning/table', 'POST', {
          tableId: meaningTableId.value,
          context,
        })
        meaningResult.value = resp.meaning
        meaningMeta.value = resp.table.displayName
        quickSetResult.value = null
        return
      }

      if (meaningMode.value === 'fusion') {
        if (!meaningFusionTable1.value || !meaningFusionTable2.value) {
          throw new Error('Select two tables first.')
        }
        const resp = await apiSend<MeaningFusionResponse>('/api/meaning/fusion', 'POST', {
          tableId1: meaningFusionTable1.value,
          tableId2: meaningFusionTable2.value,
          context,
        })
        meaningResult.value = resp.meaning
        meaningMeta.value = `${resp.table1.displayName} + ${resp.table2.displayName}`
        quickSetResult.value = null
        return
      }

      if (meaningMode.value === 'quickSet') {
        if (!meaningQuickSetId.value) {
          throw new Error('Select a quick set first.')
        }
        quickSetResult.value = await apiSend<QuickSetResult>('/api/quick-sets/generate', 'POST', {
          id: meaningQuickSetId.value,
          context,
        })
        meaningResult.value = null
        meaningMeta.value = null
        return
      }

      // Handle unknown meaningMode values
      meaningResult.value = null
      meaningMeta.value = null
      quickSetResult.value = null
      throw new Error(`Unknown meaning mode: ${meaningMode.value}`)
    } finally {
      loading.meaning = false
    }
  }

  async function rollDice(expr?: string): Promise<DiceRollResponse | null> {
    const expression = (expr ?? diceExpression.value).trim()
    if (!expression) return null

    loading.diceRoll = true
    try {
      diceResult.value = await apiSend<DiceRollResponse>('/api/dice-roll', 'POST', { expression })
      diceExpression.value = ''
      return diceResult.value
    } finally {
      loading.diceRoll = false
    }
  }

  function clearResults() {
    fateResult.value = null
    sceneResult.value = null
    randomResult.value = null
    meaningResult.value = null
    meaningMeta.value = null
    quickSetResult.value = null
    diceResult.value = null
  }

  function initMeaningDefaults(firstTable: string, fusionTables: [string, string], firstQuickSet: string) {
    if (!meaningTableId.value && firstTable) {
      meaningTableId.value = firstTable
    }
    if (!meaningFusionTable1.value && fusionTables[0]) {
      meaningFusionTable1.value = fusionTables[0]
    }
    if (!meaningFusionTable2.value && fusionTables[1]) {
      meaningFusionTable2.value = fusionTables[1]
    }
    if (!meaningQuickSetId.value && firstQuickSet) {
      meaningQuickSetId.value = firstQuickSet
    }
  }

  return {
    // Fate Check
    fateOdds,
    fateQuestion,
    fateResult,
    runFateCheck,

    // Scene Check
    sceneContext,
    sceneResult,
    runSceneCheck,

    // Random Event
    randomResult,
    runRandomEvent,

    // Meaning
    meaningMode,
    meaningContext,
    meaningTableId,
    meaningFusionTable1,
    meaningFusionTable2,
    meaningQuickSetId,
    meaningResult,
    meaningMeta,
    quickSetResult,
    runMeaning,
    initMeaningDefaults,

    // Dice
    diceExpression,
    diceResult,
    rollDice,

    // Shared
    loading,
    clearResults,
  }
}
