<script setup lang="ts">
import { useToolNav } from '../composables/useToolNav'
import ToolsSidebar from '../components/tools/ToolsSidebar.vue'
import MythicWorkspace from '../components/tools/MythicWorkspace.vue'
import NameGeneratorPage from '../components/tools/NameGeneratorPage.vue'
import ComingSoonPage from '../components/tools/ComingSoonPage.vue'

const { activePage } = useToolNav()
</script>

<template>
  <div class="flex min-h-[calc(100vh-10rem)]">
    <ToolsSidebar />

    <!-- Content area -->
    <div class="min-w-0 flex-1">
      <MythicWorkspace v-if="activePage === 'mythic-2e'" />
      <NameGeneratorPage v-else-if="activePage === 'name-generator'" />
      <ComingSoonPage v-else :page-id="activePage" />
    </div>
  </div>
</template>
