<template>
  <LemonPageHeader title="系统设置" description="配置由数据库统一管理；只有超级管理员可修改">
    <el-tag v-if="!auth.isSuperAdmin" type="info">只读</el-tag>
  </LemonPageHeader>
  <div class="page-card">
    <el-table :data="rows" border>
      <el-table-column prop="settingGroup" label="分组" width="120" />
      <el-table-column prop="settingKey" label="配置键" min-width="250" />
      <el-table-column label="配置值" min-width="260">
        <template #default="{ row }">
          <el-switch
            v-if="row.valueType === 'bool'"
            :model-value="row.settingValue === 'true'"
            :disabled="!auth.isSuperAdmin"
            @change="(value: string | number | boolean) => row.settingValue = Boolean(value) ? 'true' : 'false'"
          />
          <el-input v-else v-model="row.settingValue" :disabled="!auth.isSuperAdmin" />
        </template>
      </el-table-column>
      <el-table-column prop="valueType" label="类型" width="100" />
      <el-table-column prop="description" label="说明" min-width="240" />
      <el-table-column v-if="auth.isSuperAdmin" label="操作" width="90">
        <template #default="{ row }"><el-button link type="primary" @click="save(row)">保存</el-button></template>
      </el-table-column>
    </el-table>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { ElMessage } from 'element-plus'
import LemonPageHeader from '@/components/lemon/LemonPageHeader.vue'
import { fetchSettings, updateSetting } from '@/api/system'
import { useAuthStore } from '@/stores/auth'
import type { Setting, SystemFeatureFlags } from '@/types/api'

const auth = useAuthStore()
const rows = ref<Setting[]>([])
async function load() { rows.value = await fetchSettings() }
async function save(row: Setting) {
  await updateSetting(row.settingKey, {
    settingGroup: row.settingGroup,
    settingValue: row.settingValue,
    valueType: row.valueType,
    description: row.description,
    isPublic: row.isPublic
  })
  auth.updateFeatures(extractFlags(rows.value))
  ElMessage.success('已保存')
}
function extractFlags(settings: Setting[]): SystemFeatureFlags {
  const read = (key: string, fallback: boolean) => {
    const value = settings.find(item => item.settingKey === key)?.settingValue
    return value === undefined ? fallback : value === 'true'
  }
  return {
    accountSwitchEnabled: read('security.account_switch_enabled', false),
    themeSwitchEnabled: read('ui.theme_switch_enabled', true),
    fontSizeSwitchEnabled: read('ui.font_size_switch_enabled', true)
  }
}
onMounted(load)
</script>
