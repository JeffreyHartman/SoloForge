<script setup lang="ts">
import { useId } from 'vue'

const inputId = useId()

defineProps<{
  label?: string
  placeholder?: string
  disabled?: boolean
  type?: 'text' | 'number'
  min?: number
  max?: number
}>()

const model = defineModel<string | number>()

defineEmits<{
  enter: []
}>()
</script>

<template>
  <div>
    <label v-if="label" :for="inputId" class="block text-xs font-medium text-[var(--color-text-muted)]">{{ label }}</label>
    <input
      :id="inputId"
      v-model="model"
      :type="type ?? 'text'"
      :placeholder="placeholder"
      :disabled="disabled"
      :min="min"
      :max="max"
      class="mt-1 w-full rounded-xl border border-[var(--color-border-primary)] bg-[var(--color-bg-input)] px-3 py-2 text-sm text-[var(--color-text-primary)] shadow-sm outline-none transition placeholder:text-[var(--color-text-dimmed)] focus:border-[var(--color-text-dimmed)] focus:shadow"
      @keydown.enter="$emit('enter')"
    />
  </div>
</template>
