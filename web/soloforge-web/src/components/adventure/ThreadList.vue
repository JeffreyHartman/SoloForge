<script setup lang="ts">
import type { Thread } from '../../types'
import BaseButton from '../common/BaseButton.vue'
import BaseInput from '../common/BaseInput.vue'
import { ref } from 'vue'

const props = defineProps<{
  activeThreads: Thread[]
  closedThreads: Thread[]
  loadingAdd: boolean
  loadingClose: boolean
  loadingReopen: boolean
  apiOnline: boolean
}>()

const emit = defineEmits<{
  add: [name: string, description: string]
  close: [name: string]
  reopen: [name: string]
}>()

const threadName = ref('')
const threadDescription = ref('')

function handleAdd() {
  if (props.loadingAdd) return
  if (!props.apiOnline) return
  const name = threadName.value.trim()
  if (name) {
    emit('add', name, threadDescription.value.trim())
    threadName.value = ''
    threadDescription.value = ''
  }
}
</script>

<template>
  <div>
    <div class="text-xs font-semibold text-[var(--color-text-secondary)]">Threads</div>
    <div class="mt-2 rounded-2xl border border-[var(--color-border-primary)] bg-[var(--color-bg-card-solid)]">
      <div class="p-3">
        <div class="text-[11px] font-semibold text-[var(--color-text-dimmed)]">Active</div>
        <div v-if="activeThreads.length === 0" class="mt-1 text-sm text-[var(--color-text-muted)]">
          (No active threads)
        </div>
        <ul v-else class="mt-2 space-y-2">
          <li v-for="t in activeThreads" :key="t.name" class="flex items-center justify-between gap-3">
            <div class="min-w-0">
              <div class="truncate text-sm font-semibold text-[var(--color-text-primary)]">{{ t.name }}</div>
              <div v-if="t.description" class="mt-0.5 truncate text-xs text-[var(--color-text-muted)]">{{ t.description }}</div>
            </div>
            <BaseButton
              variant="warning"
              size="sm"
              :disabled="loadingClose || !apiOnline"
              @click="$emit('close', t.name)"
            >
              Resolve
            </BaseButton>
          </li>
        </ul>

        <div class="mt-4 text-[11px] font-semibold text-[var(--color-text-dimmed)]">Closed</div>
        <div v-if="closedThreads.length === 0" class="mt-1 text-sm text-[var(--color-text-muted)]">
          (No closed threads)
        </div>
        <ul v-else class="mt-2 space-y-2">
          <li v-for="t in closedThreads" :key="t.name" class="flex items-center justify-between gap-3">
            <div class="min-w-0">
              <div class="truncate text-sm font-semibold text-[var(--color-text-primary)]">{{ t.name }}</div>
              <div v-if="t.description" class="mt-0.5 truncate text-xs text-[var(--color-text-muted)]">{{ t.description }}</div>
            </div>
            <BaseButton
              variant="secondary"
              size="sm"
              :disabled="loadingReopen || !apiOnline"
              @click="$emit('reopen', t.name)"
            >
              Reopen
            </BaseButton>
          </li>
        </ul>
      </div>
    </div>

    <div class="mt-3 grid grid-cols-1 gap-2">
      <BaseInput
        v-model="threadName"
        placeholder="Thread name"
        @enter="handleAdd"
      />
      <BaseInput
        v-model="threadDescription"
        placeholder="Description (optional)"
        @enter="handleAdd"
      />
      <BaseButton
        :disabled="loadingAdd || !threadName.trim() || !apiOnline"
        :loading="loadingAdd"
        @click="handleAdd"
      >
        Add thread
      </BaseButton>
    </div>
  </div>
</template>
