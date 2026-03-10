<script setup lang="ts">
import { useToolNav } from '../../composables/useToolNav'

const { activePage, sidebarOpen, groups, selectPage, toggleSidebar, toggleGroup, isGroupCollapsed } = useToolNav()
</script>

<template>
  <!-- Toggle button (always visible) -->
  <button
    class="absolute top-3 z-30 rounded-r-lg border border-l-0 border-[var(--color-border-primary)] bg-[var(--color-bg-card-solid)] px-1.5 py-3 text-[var(--color-text-dimmed)] shadow-sm transition-all duration-300 ease-in-out hover:text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)]"
    :style="{ left: sidebarOpen ? '240px' : '0' }"
    :aria-expanded="sidebarOpen"
    aria-label="Toggle tools sidebar"
    @click="toggleSidebar"
  >
    <svg
      class="h-4 w-4 transition-transform duration-200"
      :class="{ 'rotate-180': !sidebarOpen }"
      viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2"
    >
      <path d="M10 4l-4 4 4 4" />
    </svg>
  </button>

  <!-- Sidebar drawer -->
  <div
    class="shrink-0 overflow-hidden transition-all duration-300 ease-in-out"
    :style="{ width: sidebarOpen ? '240px' : '0px' }"
  >
    <div class="flex h-full w-[240px] flex-col border-r border-[var(--color-border-primary)] bg-[var(--color-bg-card-solid)]" :inert="!sidebarOpen">
      <!-- Header -->
      <div class="border-b border-[var(--color-border-primary)] px-3 py-2.5">
        <span class="text-xs font-semibold uppercase tracking-wider text-[var(--color-text-muted)]">Tools</span>
      </div>

      <!-- Groups with pages -->
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
    </div>
  </div>
</template>
