// API Response Types

export interface ApiState {
  session: SessionState
  currentCampaign?: CampaignInfo | null
  adventure: AdventureState
  historyCount: number
}

export interface SessionState {
  chaos: number
  engine: string
  theme: string
  lastQuickRoll?: string | null
}

export interface CampaignInfo {
  id: string
  name: string
  createdAt: string
  lastPlayed: string
  historyCount: number
  autoJournalEvents: boolean
  autoJournalDiceRolls: boolean
}

export interface CampaignSummary {
  id: string
  name: string
  createdAt: string
  lastPlayed: string
  chaos: number
  engine: string
  theme: string
  characterCount: number
  activeThreadCount: number
  closedThreadCount: number
  historyCount: number
}

export interface AdventureState {
  characters: Character[]
  activeThreads: Thread[]
  closedThreads: Thread[]
}

export interface Character {
  name: string
  description?: string | null
  createdAt: string
}

export interface Thread {
  name: string
  description?: string | null
  createdAt: string
  closedAt?: string | null
}

// Mythic Engine Types

export interface FateCheckResponse {
  chaos: number
  odds: string
  fate: {
    roll: number
    result: string
    randomEventTriggered: boolean
  }
  randomEvent?: RandomEventResult | null
}

export interface SceneCheckResponse {
  chaos: number
  scene: {
    roll: number
    result: string
    sceneAdjustment?: string | null
    randomEvent?: RandomEventResult | null
  }
}

export interface RandomEventResult {
  eventFocus: string
  eventAction: string
  selectedCharacter?: string | null
  selectedThread?: string | null
  isNewNpc: boolean
  listWasEmpty: boolean
}

// Tables and Meaning Types

export interface TableInfo {
  id: string
  displayName: string
  isElement: boolean
  category: string
}

export interface TableGroup {
  label: string
  items: TableInfo[]
}

export interface ThemeSummary {
  name: string
  description: string
}

export interface MeaningResult {
  tableName: string
  word1: string
  word2: string
  isFusion: boolean
  combined: string
}

export interface MeaningTableResponse {
  table: { id: string; displayName: string }
  meaning: MeaningResult
}

export interface MeaningFusionResponse {
  table1: { id: string; displayName: string }
  table2: { id: string; displayName: string }
  meaning: MeaningResult
}

export interface QuickSet {
  id: string
  name: string
  description: string
  steps: { label: string; table: string; count: number }[]
}

export interface QuickSetResult {
  quickSet: QuickSet
  results: { label: string; words: string[]; combined: string; tableId: string }[]
}

// History Types

export interface HistoryEntry {
  id: string
  timestamp: string
  type: string
  context?: string | null
  result: string
  details?: string | null
}

// Dice Types

export interface DiceRollResponse {
  roll: {
    summary: string
    total: number
    diceTotal: number
    modifier: number
    terms: { count: number; faces: number; sign: number; rolls: number[]; total: number }[]
  }
  breakdown: string
}

// Notes Vault Types

export interface NoteNode {
  name: string
  path: string
  isFolder: boolean
  children: NoteNode[]
}

export interface NoteTreeResponse {
  campaignId: string
  sessionLogPath: string
  tree: NoteNode[]
}

export interface NoteListResponse {
  campaignId: string
  paths: string[]
}

// Tool Navigation Types

export interface ToolPage {
  id: string
  name: string
  comingSoon?: boolean
}

export interface ToolGroup {
  id: string
  name: string
  pages: ToolPage[]
  sortOrder: number
}

// UI Types

export type MeaningMode = 'action' | 'description' | 'table' | 'fusion' | 'quickSet'

export type ViewName = 'dashboard' | 'tools' | 'adventure' | 'journal' | 'history'

export interface OddsOption {
  value: string
  label: string
}

export const ODDS_OPTIONS: OddsOption[] = [
  { value: 'Impossible', label: 'Impossible' },
  { value: 'NearlyImpossible', label: 'Nearly Impossible' },
  { value: 'VeryUnlikely', label: 'Very Unlikely' },
  { value: 'Unlikely', label: 'Unlikely' },
  { value: 'FiftyFifty', label: '50/50' },
  { value: 'Likely', label: 'Likely' },
  { value: 'VeryLikely', label: 'Very Likely' },
  { value: 'NearlyCertain', label: 'Nearly Certain' },
  { value: 'Certain', label: 'Certain' },
]

export const QUICK_DICE = ['d4', 'd6', 'd8', 'd10', 'd12', 'd20', 'd100'] as const
