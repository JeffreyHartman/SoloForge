<script setup lang="ts">
import type {
  FateCheckResponse,
  SceneCheckResponse,
  RandomEventResult,
  MeaningResult,
  QuickSetResult,
  DiceRollResponse,
  TableGroup,
  QuickSet,
  MeaningMode
} from '../types'
import FateCheckPanel from '../components/mythic/FateCheckPanel.vue'
import SceneCheckPanel from '../components/mythic/SceneCheckPanel.vue'
import RandomEventPanel from '../components/mythic/RandomEventPanel.vue'
import MeaningPanel from '../components/mythic/MeaningPanel.vue'
import DiceRollerPanel from '../components/mythic/DiceRollerPanel.vue'

defineProps<{
  chaos: number
  fateResult: FateCheckResponse | null
  sceneResult: SceneCheckResponse | null
  randomResult: RandomEventResult | null
  meaningResult: MeaningResult | null
  meaningMeta: string | null
  quickSetResult: QuickSetResult | null
  diceResult: DiceRollResponse | null
  tableGroups: TableGroup[]
  quickSets: QuickSet[]
  loadingFate: boolean
  loadingScene: boolean
  loadingRandom: boolean
  loadingMeaning: boolean
  loadingDice: boolean
  loadingAddNpc: boolean
  apiOnline: boolean
}>()

const fateOdds = defineModel<string>('fateOdds')
const fateQuestion = defineModel<string>('fateQuestion')
const sceneContext = defineModel<string>('sceneContext')
const meaningMode = defineModel<MeaningMode>('meaningMode')
const meaningContext = defineModel<string>('meaningContext')
const meaningTableId = defineModel<string>('meaningTableId')
const meaningFusionTable1 = defineModel<string>('meaningFusionTable1')
const meaningFusionTable2 = defineModel<string>('meaningFusionTable2')
const meaningQuickSetId = defineModel<string>('meaningQuickSetId')
const diceExpression = defineModel<string>('diceExpression')

const emit = defineEmits<{
  fateCheck: []
  sceneCheck: []
  randomEvent: []
  meaning: []
  diceRoll: [expr?: string]
  addNpc: [name: string, description: string]
}>()
</script>

<template>
  <div class="grid grid-cols-1 gap-6 lg:grid-cols-2">
    <div class="space-y-6">
      <FateCheckPanel
        :chaos="chaos"
        :result="fateResult"
        :loading="loadingFate"
        :api-online="apiOnline"
        v-model:odds="fateOdds"
        v-model:question="fateQuestion"
        @roll="emit('fateCheck')"
      />

      <SceneCheckPanel
        :chaos="chaos"
        :result="sceneResult"
        :loading="loadingScene"
        :api-online="apiOnline"
        v-model:context="sceneContext"
        @roll="emit('sceneCheck')"
      />

      <RandomEventPanel
        :result="randomResult"
        :loading="loadingRandom"
        :loading-add-npc="loadingAddNpc"
        :api-online="apiOnline"
        @roll="emit('randomEvent')"
        @add-npc="(name, desc) => emit('addNpc', name, desc)"
      />
    </div>

    <div class="space-y-6">
      <MeaningPanel
        :table-groups="tableGroups"
        :quick-sets="quickSets"
        :meaning-result="meaningResult"
        :meaning-meta="meaningMeta"
        :quick-set-result="quickSetResult"
        :loading="loadingMeaning"
        :api-online="apiOnline"
        v-model:mode="meaningMode"
        v-model:context="meaningContext"
        v-model:table-id="meaningTableId"
        v-model:fusion-table1="meaningFusionTable1"
        v-model:fusion-table2="meaningFusionTable2"
        v-model:quick-set-id="meaningQuickSetId"
        @roll="emit('meaning')"
      />

      <DiceRollerPanel
        :result="diceResult"
        :loading="loadingDice"
        :api-online="apiOnline"
        v-model:expression="diceExpression"
        @roll="(expr) => emit('diceRoll', expr)"
      />
    </div>
  </div>
</template>
