<script setup lang="ts">
import FateCheckPanel from '../mythic/FateCheckPanel.vue'
import SceneCheckPanel from '../mythic/SceneCheckPanel.vue'
import RandomEventPanel from '../mythic/RandomEventPanel.vue'
import MeaningPanel from '../mythic/MeaningPanel.vue'
import DiceRollerPanel from '../mythic/DiceRollerPanel.vue'
</script>

<template>
  <div class="grid grid-cols-1 gap-6 lg:grid-cols-2">
    <div class="space-y-6">
      <FateCheckPanel />
      <SceneCheckPanel />
      <RandomEventPanel />
    </div>
    <div class="space-y-6">
      <MeaningPanel />
      <DiceRollerPanel />
    </div>
  </div>
</template>
