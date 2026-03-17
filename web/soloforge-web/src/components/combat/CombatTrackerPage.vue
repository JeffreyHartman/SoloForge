<script setup lang="ts">
import CombatTrackerPanel from './CombatTrackerPanel.vue'
</script>

<template>
  <div>
    <div class="rounded-3xl border border-[var(--color-border-secondary)] bg-[var(--color-bg-card)] p-5 shadow-sm backdrop-blur">
      <h2 class="mb-4 text-lg font-semibold text-[var(--color-text-primary)]">Combat Tracker</h2>
      <CombatTrackerPanel />
    </div>
  </div>
</template>
