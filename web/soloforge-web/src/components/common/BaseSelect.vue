<script setup lang="ts">
import { useId } from 'vue'
import type { TableGroup } from '../../types'

const selectId = useId()

defineProps<{
  label?: string
  options?: { value: string; label: string }[]
  groups?: TableGroup[]
  disabled?: boolean
}>()

const model = defineModel<string>()
</script>

<template>
  <div>
    <label v-if="label" :for="selectId" class="block text-xs font-medium text-[var(--color-text-muted)]">{{ label }}</label>
    <select
      :id="selectId"
      v-model="model"
      :disabled="disabled"
      class="mt-1 w-full rounded-xl border border-[var(--color-border-primary)] bg-[var(--color-bg-input)] px-3 py-2 text-sm text-[var(--color-text-primary)] shadow-sm outline-none transition focus:border-[var(--color-text-dimmed)] focus:shadow"
    >
      <template v-if="groups && groups.length > 0">
        <optgroup v-for="g in groups" :key="g.label" :label="g.label">
          <option v-for="t in g.items" :key="t.id" :value="t.id">{{ t.displayName }}</option>
        </optgroup>
      </template>
      <template v-else-if="options && options.length > 0">
        <option v-for="o in options" :key="o.value" :value="o.value">{{ o.label }}</option>
      </template>
      <slot v-else />
    </select>
  </div>
</template>
