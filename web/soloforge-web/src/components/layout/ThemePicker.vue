<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount } from 'vue'
import { useTheme } from '../../composables/useTheme'

const { themes, currentThemeId, setTheme } = useTheme()
const open = ref(false)
const pickerRef = ref<HTMLElement | null>(null)
const menuId = 'theme-menu'

function selectTheme(id: string) {
  setTheme(id)
  open.value = false
}

function handleClickOutside(e: MouseEvent) {
  if (pickerRef.value && !pickerRef.value.contains(e.target as Node)) {
    open.value = false
  }
}

function handleKeydown(e: KeyboardEvent) {
  if (e.key === 'Escape') {
    open.value = false
  }
}

onMounted(() => {
  document.addEventListener('mousedown', handleClickOutside)
  document.addEventListener('keydown', handleKeydown)
})
onBeforeUnmount(() => {
  document.removeEventListener('mousedown', handleClickOutside)
  document.removeEventListener('keydown', handleKeydown)
})
</script>

<template>
  <div ref="pickerRef" class="relative">
    <button
      type="button"
      class="flex items-center gap-1.5 rounded-full border border-[var(--color-border-secondary)] bg-[var(--color-bg-card)] px-3 py-1.5 text-xs font-medium text-[var(--color-text-secondary)] shadow-sm backdrop-blur transition hover:bg-[var(--color-bg-card-solid)]"
      title="Change theme"
      aria-label="Change theme"
      aria-haspopup="menu"
      :aria-expanded="open"
      :aria-controls="menuId"
      @click="open = !open"
    >
      <!-- Palette icon -->
      <svg class="h-4 w-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
        <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10c.83 0 1.5-.67 1.5-1.5 0-.39-.15-.74-.39-1.01-.23-.26-.38-.61-.38-1 0-.83.67-1.5 1.5-1.5H16c3.31 0 6-2.69 6-6 0-5.52-4.48-9.99-10-9.99z" />
        <circle cx="6.5" cy="11.5" r="1.5" fill="currentColor" />
        <circle cx="9.5" cy="7.5" r="1.5" fill="currentColor" />
        <circle cx="14.5" cy="7.5" r="1.5" fill="currentColor" />
        <circle cx="17.5" cy="11.5" r="1.5" fill="currentColor" />
      </svg>
      <span class="hidden sm:inline">Theme</span>
    </button>

    <!-- Dropdown -->
    <Transition
      enter-active-class="transition duration-150 ease-out"
      enter-from-class="opacity-0 scale-95 translate-y-1"
      enter-to-class="opacity-100 scale-100 translate-y-0"
      leave-active-class="transition duration-100 ease-in"
      leave-from-class="opacity-100 scale-100 translate-y-0"
      leave-to-class="opacity-0 scale-95 translate-y-1"
    >
      <div
        v-if="open"
        :id="menuId"
        class="absolute right-0 z-50 mt-2 w-72 origin-top-right rounded-2xl border border-[var(--color-border-primary)] bg-[var(--color-bg-card-solid)] p-2 shadow-xl"
        role="menu"
        aria-label="Theme selection"
      >
        <div class="max-h-[420px] overflow-y-auto">
          <button
            v-for="theme in themes"
            :key="theme.id"
            type="button"
            role="menuitemradio"
            :aria-checked="currentThemeId === theme.id"
            class="flex w-full items-center gap-3 rounded-xl px-3 py-2.5 text-left transition"
            :class="
              currentThemeId === theme.id
                ? 'bg-[var(--color-bg-accent)] text-[var(--color-text-inverted)]'
                : 'text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)]'
            "
            @click="selectTheme(theme.id)"
          >
            <!-- Color preview swatches -->
            <div class="flex shrink-0 items-center -space-x-1">
              <span
                class="inline-block h-5 w-5 rounded-full ring-2"
                :class="currentThemeId === theme.id ? 'ring-[var(--color-text-inverted)]/30' : 'ring-[var(--color-bg-card-solid)]'"
                :style="{ backgroundColor: theme.preview.bg }"
              />
              <span
                class="inline-block h-5 w-5 rounded-full ring-2"
                :class="currentThemeId === theme.id ? 'ring-[var(--color-text-inverted)]/30' : 'ring-[var(--color-bg-card-solid)]'"
                :style="{ backgroundColor: theme.preview.card }"
              />
              <span
                class="inline-block h-5 w-5 rounded-full ring-2"
                :class="currentThemeId === theme.id ? 'ring-[var(--color-text-inverted)]/30' : 'ring-[var(--color-bg-card-solid)]'"
                :style="{ backgroundColor: theme.preview.accent }"
              />
            </div>

            <!-- Theme info -->
            <div class="min-w-0 flex-1">
              <div class="text-sm font-semibold leading-tight">{{ theme.name }}</div>
              <div
                class="text-[11px] leading-tight"
                :class="
                  currentThemeId === theme.id
                    ? 'opacity-70'
                    : 'text-[var(--color-text-dimmed)]'
                "
              >
                {{ theme.genre }}
              </div>
            </div>

            <!-- Active check -->
            <svg
              v-if="currentThemeId === theme.id"
              class="h-4 w-4 shrink-0"
              viewBox="0 0 16 16"
              fill="none"
              stroke="currentColor"
              stroke-width="2.5"
            >
              <path d="M3 8l3.5 3.5 6.5-7" />
            </svg>
          </button>
        </div>
      </div>
    </Transition>
  </div>
</template>
