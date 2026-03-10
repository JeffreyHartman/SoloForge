import { ref, computed } from 'vue'
import { NAME_STYLES } from '../data/names'

const selectedStyleId = ref<string>(NAME_STYLES[0]?.id ?? 'fantasy')
const count = ref<number>(5)
const results = ref<string[]>([])

export function useNameGenerator() {
  const styles = NAME_STYLES

  const selectedStyle = computed(() =>
    NAME_STYLES.find(s => s.id === selectedStyleId.value) ?? NAME_STYLES[0]
  )

  function generate() {
    const style = selectedStyle.value
    if (!style) return

    const pool = style.names
    const n = Math.min(Math.max(1, count.value), pool.length)
    const shuffled = [...pool].sort(() => Math.random() - 0.5)
    results.value = shuffled.slice(0, n)
  }

  function clear() {
    results.value = []
  }

  return {
    selectedStyleId,
    count,
    results,
    styles,
    selectedStyle,
    generate,
    clear,
  }
}
