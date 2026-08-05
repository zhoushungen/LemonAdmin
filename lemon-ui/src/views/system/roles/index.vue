<template>
  <LemonPageHeader title="角色权限" description="功能权限决定能做什么，数据范围决定能看到哪些数据">
    <el-button v-if="auth.hasPermission('system.role.create')" type="primary" @click="create">新增角色</el-button>
  </LemonPageHeader>
  <div class="page-card">
    <LemonTable table-key="system-roles" export-name="角色" :rows="rows" :columns="columns" @refresh="load">
      <template #cell-dataScope="{ row }"><el-tag>{{ dataScopeLabel(row.dataScope) }}</el-tag></template>
      <template #cell-isEnabled="{ row }"><el-tag :type="row.isEnabled ? 'success' : 'info'">{{ row.isEnabled ? '启用' : '禁用' }}</el-tag></template>
      <template v-if="auth.hasPermission('system.role.update')" #actions>
        <el-table-column fixed="right" label="操作" width="160">
          <template #default="{ row }">
            <el-button link type="primary" @click="edit(row)">编辑</el-button>
            <el-button link type="primary" @click="grant(row)">分配权限</el-button>
          </template>
        </el-table-column>
      </template>
    </LemonTable>
  </div>

  <el-dialog v-model="dialog" :title="editing ? '编辑角色' : '新增角色'" width="560px">
    <el-form label-width="100px">
      <el-form-item label="角色代码"><el-input v-model="form.code" :disabled="!!editing" placeholder="例如 content_admin" /></el-form-item>
      <el-form-item label="角色名称"><el-input v-model="form.name" placeholder="名称可自由填写，包括“管理员”" /></el-form-item>
      <el-form-item label="数据范围">
        <el-select v-model="form.dataScope" class="w-full">
          <el-option v-for="item in dataScopeOptions" :key="item.value" :label="item.label" :value="item.value" />
        </el-select>
        <div class="text-muted">超级管理员不读取此配置，始终拥有全部数据。</div>
      </el-form-item>
      <el-form-item label="说明"><el-input v-model="form.description" type="textarea" /></el-form-item>
      <el-form-item v-if="editing" label="启用"><el-switch v-model="form.isEnabled" /></el-form-item>
    </el-form>
    <template #footer><el-button @click="dialog = false">取消</el-button><el-button type="primary" @click="save">保存</el-button></template>
  </el-dialog>

  <el-dialog v-model="grantDialog" title="分配功能权限" width="700px">
    <div v-for="(group, module) in grouped" :key="module" class="permission-group">
      <h4>{{ module }}</h4>
      <el-checkbox-group v-model="selected">
        <el-checkbox v-for="permission in group" :key="permission.id" :value="permission.id">
          {{ permission.name }} <span class="text-muted">{{ permission.code }}</span>
        </el-checkbox>
      </el-checkbox-group>
    </div>
    <template #footer><el-button @click="grantDialog = false">取消</el-button><el-button type="primary" @click="saveGrant">保存权限</el-button></template>
  </el-dialog>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { ElMessage } from 'element-plus'
import LemonPageHeader from '@/components/lemon/LemonPageHeader.vue'
import LemonTable from '@/components/lemon/LemonTable.vue'
import { createRole, fetchPermissions, fetchRoles, updateRole, updateRolePermissions } from '@/api/system'
import { useAuthStore } from '@/stores/auth'
import { DataScopeType, type Permission, type Role } from '@/types/api'
import type { LemonColumn } from '@/types/table'

const auth = useAuthStore()
const rows = ref<Role[]>([])
const permissions = ref<Permission[]>([])
const dialog = ref(false)
const grantDialog = ref(false)
const editing = ref<number>()
const selected = ref<number[]>([])
const grantRole = ref<Role>()
const form = reactive({ code: '', name: '', description: '', dataScope: DataScopeType.Self, isEnabled: true })

const dataScopeOptions = [
  { value: DataScopeType.All, label: '全部数据' },
  { value: DataScopeType.DepartmentAndChildren, label: '主部门及下级部门' },
  { value: DataScopeType.Department, label: '仅主部门' },
  { value: DataScopeType.ManagedDepartments, label: '主管部门及下级部门' },
  { value: DataScopeType.Self, label: '仅本人' }
]

const columns: LemonColumn<Role>[] = [
  { key: 'name', label: '角色名称', fixed: 'left' },
  { key: 'code', label: '角色代码' },
  { key: 'dataScope', label: '数据范围', minWidth: 170 },
  { key: 'description', label: '说明', minWidth: 220 },
  { key: 'isEnabled', label: '状态' }
]

const grouped = computed(() => permissions.value.reduce<Record<string, Permission[]>>((result, permission) => {
  ;(result[permission.module] ??= []).push(permission)
  return result
}, {}))

function dataScopeLabel(value: DataScopeType) {
  return dataScopeOptions.find(item => item.value === value)?.label ?? '仅本人'
}

async function load() {
  ;[rows.value, permissions.value] = await Promise.all([fetchRoles(), fetchPermissions()])
}

function create() {
  editing.value = undefined
  Object.assign(form, { code: '', name: '', description: '', dataScope: DataScopeType.Self, isEnabled: true })
  dialog.value = true
}

function edit(role: Role) {
  editing.value = role.id
  Object.assign(form, role)
  dialog.value = true
}

async function save() {
  if (editing.value) await updateRole(editing.value, form)
  else await createRole({ ...form, permissionIds: [] })
  dialog.value = false
  ElMessage.success('保存成功')
  load()
}

function grant(role: Role) {
  grantRole.value = role
  selected.value = [...role.permissionIds]
  grantDialog.value = true
}

async function saveGrant() {
  await updateRolePermissions(grantRole.value!.id, selected.value)
  grantDialog.value = false
  ElMessage.success('权限已更新')
  load()
}

onMounted(load)
</script>

<style scoped>
.permission-group { border-bottom: 1px solid #e5e7eb; padding: 8px 0 14px }
.el-checkbox { margin-bottom: 8px; min-width: 260px }
</style>
