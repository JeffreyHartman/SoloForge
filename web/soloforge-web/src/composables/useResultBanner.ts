import { ref } from 'vue'
import type {
  FateCheckResponse,
  SceneCheckResponse,
  RandomEventResult,
  MeaningResult,
  QuickSetResult,
  DiceRollResponse,
} from '../types'

export type ResultType = 'fate' | 'scene' | 'event' | 'meaning' | 'dice'

export interface ResultBannerItem {
  id: string
  type: ResultType
  title: string
  detail?: string
  subDetail?: string
}

let nextId = 0
let timer: ReturnType<typeof setTimeout> | null = null

const banner = ref<ResultBannerItem | null>(null)
const lastEvent = ref<ResultBannerItem | null>(null)

function showBanner(item: Omit<ResultBannerItem, 'id'>) {
  if (timer) clearTimeout(timer)
  const entry: ResultBannerItem = { ...item, id: `rb-${++nextId}` }
  banner.value = entry
  lastEvent.value = entry
  timer = setTimeout(() => dismissBanner(), 5000)
}

function dismissBanner() {
  banner.value = null
  if (timer) {
    clearTimeout(timer)
    timer = null
  }
}

function clearLastEvent() {
  lastEvent.value = null
}

// --- Format helpers ---

export function formatFateResult(r: FateCheckResponse): Omit<ResultBannerItem, 'id'> {
  const detail = [
    r.fate.roll != null ? `Roll ${r.fate.roll}` : null,
    `Odds ${r.odds}`,
    `Chaos ${r.chaos}`,
  ].filter(Boolean).join(' · ')

  const subParts: string[] = []
  if (r.randomEvent) {
    subParts.push(`Random Event: ${r.randomEvent.eventFocus} — ${r.randomEvent.eventAction}`)
  }

  return {
    type: 'fate',
    title: r.fate.result,
    detail,
    subDetail: subParts.length ? subParts.join('; ') : undefined,
  }
}

export function formatSceneResult(r: SceneCheckResponse): Omit<ResultBannerItem, 'id'> {
  const detail = [
    r.scene.roll != null ? `Roll ${r.scene.roll}` : null,
    `Chaos ${r.chaos}`,
  ].filter(Boolean).join(' · ')

  const subParts: string[] = []
  if (r.scene.sceneAdjustment) {
    subParts.push(`Adjustment: ${r.scene.sceneAdjustment}`)
  }
  if (r.scene.randomEvent) {
    subParts.push(`Random Event: ${r.scene.randomEvent.eventFocus} — ${r.scene.randomEvent.eventAction}`)
  }

  return {
    type: 'scene',
    title: r.scene.result,
    detail,
    subDetail: subParts.length ? subParts.join('; ') : undefined,
  }
}

export function formatRandomResult(r: RandomEventResult): Omit<ResultBannerItem, 'id'> {
  const subParts: string[] = []
  if (r.selectedCharacter) subParts.push(`Character: ${r.selectedCharacter}`)
  if (r.selectedThread) subParts.push(`Thread: ${r.selectedThread}`)

  return {
    type: 'event',
    title: r.eventFocus,
    detail: r.eventAction,
    subDetail: subParts.length ? subParts.join(' · ') : undefined,
  }
}

export function formatMeaningResult(r: MeaningResult, meta?: string | null): Omit<ResultBannerItem, 'id'> {
  const detailParts: string[] = []
  if (meta) detailParts.push(meta)
  detailParts.push(`${r.word1} + ${r.word2}`)

  return {
    type: 'meaning',
    title: r.combined,
    detail: detailParts.join(' · '),
  }
}

export function formatQuickSetResult(r: QuickSetResult): Omit<ResultBannerItem, 'id'> {
  const lines = r.results.map(entry => `${entry.label}: ${entry.combined}`)

  return {
    type: 'meaning',
    title: r.quickSet.name,
    detail: lines.join(' · '),
  }
}

export function formatDiceResult(r: DiceRollResponse): Omit<ResultBannerItem, 'id'> {
  return {
    type: 'dice',
    title: `${r.roll.summary} = ${r.roll.total}`,
    detail: r.breakdown || undefined,
  }
}

export function useResultBanner() {
  return {
    banner,
    lastEvent,
    showBanner,
    dismissBanner,
    clearLastEvent,
  }
}
