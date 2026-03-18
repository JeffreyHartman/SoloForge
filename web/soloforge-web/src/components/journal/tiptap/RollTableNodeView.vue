<script setup lang="ts">
import { ref, computed } from 'vue'
import { NodeViewWrapper, nodeViewProps } from '@tiptap/vue-3'
import { STYLES, DEFAULT_STYLE, getSummary } from './rollStyles'

const props = defineProps(nodeViewProps)

const collapsed = ref(true)

const rollType = computed(() => (props.node.attrs.rollType as string) ?? '')

const fields = computed<Record<string, string>>(() => {
  try {
    return JSON.parse(props.node.attrs.fields || '{}') as Record<string, string>
  } catch {
    return {}
  }
})

const style = computed(() => STYLES[rollType.value] ?? DEFAULT_STYLE)
const summary = computed(() => getSummary(rollType.value, fields.value))
const fieldEntries = computed(() => Object.entries(fields.value))

function toggle() {
  collapsed.value = !collapsed.value
}
</script>

<template>
  <NodeViewWrapper
    class="roll-panel-wysiwyg"
    :class="{
      'ProseMirror-selectednode': selected,
      'roll-panel-wysiwyg-expanded': !collapsed,
    }"
    :style="{ borderLeft: `3px solid ${style.border}` }"
    role="button"
    tabindex="0"
    :aria-expanded="!collapsed"
    :aria-label="`${style.label} roll: ${summary.context} ${summary.result}`"
    @click="toggle"
    @keydown.enter.prevent="toggle"
    @keydown.space.prevent="toggle"
  >
    <!-- Collapsed view -->
    <template v-if="collapsed">
      <span
        class="roll-panel-wysiwyg-badge"
        :style="{ color: style.color, backgroundColor: style.bg }"
      >
        {{ style.label }}
      </span>
      <div class="roll-panel-wysiwyg-body">
        <span v-if="summary.context" class="roll-panel-wysiwyg-context">{{ summary.context }}</span>
        <span v-if="summary.result" class="roll-panel-wysiwyg-result">{{ summary.result }}</span>
      </div>
    </template>

    <!-- Expanded view -->
    <template v-else>
      <div class="roll-panel-wysiwyg-expanded-header">
        <span
          class="roll-panel-wysiwyg-badge"
          :style="{ color: style.color, backgroundColor: style.bg }"
        >
          {{ style.label }}
        </span>
        <span class="roll-panel-wysiwyg-type-label">{{ rollType }}</span>
        <button
          class="roll-panel-wysiwyg-collapse-btn"
          title="Collapse"
          aria-label="Collapse"
          @click.stop="toggle"
        >
          <svg class="roll-panel-wysiwyg-collapse-icon" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M4 10l4-4 4 4" />
          </svg>
        </button>
      </div>
      <div class="roll-panel-wysiwyg-fields">
        <div v-for="[key, val] in fieldEntries" :key="key" class="roll-panel-wysiwyg-field">
          <span class="roll-panel-wysiwyg-field-label">{{ key }}</span>
          <span class="roll-panel-wysiwyg-field-value">{{ val }}</span>
        </div>
      </div>
    </template>
  </NodeViewWrapper>
</template>
