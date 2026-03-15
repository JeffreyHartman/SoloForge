<script setup lang="ts">
import CollapsibleSidebar from '../common/CollapsibleSidebar.vue'
import { useToolNav } from '../../composables/useToolNav'

const { activePage, sidebarOpen, groups, selectPage, toggleGroup, isGroupCollapsed } = useToolNav()
</script>

<template>
  <CollapsibleSidebar :open="sidebarOpen" title="Tools" :expanded-width="240" @update:open="sidebarOpen = $event">
    <nav class="flex-1 overflow-y-auto py-1" aria-label="Tool categories">
      <div v-for="group in groups" :key="group.id" class="mb-0.5">
        <!-- Group header (click to collapse/expand) -->
        <button
          type="button"
          class="flex w-full items-center gap-1.5 px-3 py-2 text-left text-[11px] font-semibold uppercase tracking-wider text-[var(--color-text-muted)] transition hover:text-[var(--color-text-primary)]"
          :aria-expanded="!isGroupCollapsed(group.id)"
          :aria-label="`${group.name} group`"
          @click="toggleGroup(group.id)"
        >
          <svg
            class="h-3 w-3 shrink-0 transition-transform duration-200"
            :class="{ '-rotate-90': isGroupCollapsed(group.id) }"
            viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2"
          >
            <path d="M4 6l4 4 4-4" />
          </svg>
          <span>{{ group.name }}</span>
        </button>

        <!-- Page items (collapsible) -->
        <div v-if="!isGroupCollapsed(group.id)">
          <button
            v-for="page in group.pages"
            :key="page.id"
            type="button"
            class="flex w-full items-center gap-2 py-1.5 pl-8 pr-3 text-left text-sm transition"
            :class="
              activePage === page.id
                ? 'bg-[var(--color-bg-accent)] text-[var(--color-text-inverted)] font-medium'
                : page.comingSoon
                  ? 'text-[var(--color-text-dimmed)] hover:bg-[var(--color-bg-hover)]'
                  : 'text-[var(--color-text-secondary)] hover:bg-[var(--color-bg-hover)] hover:text-[var(--color-text-primary)]'
            "
            :aria-current="activePage === page.id ? 'page' : undefined"
            @click="selectPage(page.id)"
          >
            <span>{{ page.name }}</span>
            <span
              v-if="page.comingSoon && activePage !== page.id"
              class="ml-auto text-[10px] text-[var(--color-text-dimmed)]"
            >soon</span>
          </button>
        </div>
      </div>
    </nav>
  </CollapsibleSidebar>
</template>
