<template>
  <el-drawer v-model="open" title="界面设置" size="320px">
    <template v-if="auth.features.fontSizeSwitchEnabled">
      <h4>整体字号</h4>
      <el-radio-group v-model="ui.fontSize">
        <el-radio-button value="small">小</el-radio-button>
        <el-radio-button value="medium">标准</el-radio-button>
        <el-radio-button value="large">大</el-radio-button>
      </el-radio-group>
      <div class="hint">同时作用于页面、列表、查询表单、下拉项和分页。</div>
    </template>

    <template v-if="auth.features.themeSwitchEnabled">
      <h4>配色方案</h4>
      <div class="colors">
        <button
          v-for="(color, name) in ui.colors"
          :key="name"
          class="theme-dot"
          :class="{ active: ui.theme === name }"
          :style="{ background: color }"
          @click="ui.theme = name as any"
        />
      </div>
    </template>

    <el-empty
      v-if="!auth.features.fontSizeSwitchEnabled && !auth.features.themeSwitchEnabled"
      description="系统已关闭界面个性化设置"
    />
  </el-drawer>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { useUiStore } from '@/stores/ui'

const props = defineProps<{ modelValue: boolean }>()
const emit = defineEmits<{ 'update:modelValue': [boolean] }>()
const auth = useAuthStore()
const ui = useUiStore()
const open = computed({ get: () => props.modelValue, set: value => emit('update:modelValue', value) })
</script>

<style scoped>
.hint { color: #64748b; margin-top: 10px; line-height: 1.6 }
.colors { display: flex; gap: 14px }
</style>
