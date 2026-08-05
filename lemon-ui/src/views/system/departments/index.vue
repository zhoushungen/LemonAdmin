<template>
  <LemonPageHeader title="部门管理" description="管理员有一个主部门，也可以主管多个部门">
    <el-button v-if="auth.hasPermission('system.department.create')" type="primary" @click="create">新增部门</el-button>
  </LemonPageHeader>

  <div class="page-card">
    <LemonTable
      table-key="system-departments"
      export-name="部门"
      :rows="rows"
      :columns="columns"
      :importable="auth.hasPermission('system.department.create')"
      @refresh="load"
      @import="importRows"
    >
      <template #cell-isEnabled="{ row }">
        <el-tag :type="row.isEnabled ? 'success' : 'info'">{{ row.isEnabled ? '启用' : '禁用' }}</el-tag>
      </template>
      <template v-if="canWrite" #actions>
        <el-table-column fixed="right" label="操作" width="150">
          <template #default="{ row }">
            <el-button v-if="auth.hasPermission('system.department.update')" link type="primary" @click="edit(row)">编辑</el-button>
            <el-button v-if="auth.hasPermission('system.department.delete')" link type="danger" @click="remove(row)">删除</el-button>
          </template>
        </el-table-column>
      </template>
    </LemonTable>
  </div>

  <el-dialog v-model="dialog" :title="editing ? '编辑部门' : '新增部门'" width="560px">
    <el-form label-width="100px">
      <el-form-item label="上级部门">
        <el-select v-model="form.parentId" clearable class="w-full">
          <el-option v-for="department in parentOptions" :key="department.id" :label="department.name" :value="department.id" />
        </el-select>
      </el-form-item>
      <el-form-item label="部门名称"><el-input v-model="form.name" /></el-form-item>
      <el-form-item label="部门代码"><el-input v-model="form.code" /></el-form-item>
      <el-form-item label="部门主管">
        <el-select v-model="form.managerAdminId" clearable filterable class="w-full" placeholder="可不设置">
          <el-option
            v-for="manager in managerOptions"
            :key="manager.id"
            :label="`${manager.displayName}（${manager.username}）`"
            :value="manager.id"
          />
        </el-select>
        <div class="text-muted">同一管理员可以主管多个部门。</div>
      </el-form-item>
      <el-form-item label="联系电话"><el-input v-model="form.phone" /></el-form-item>
      <el-form-item label="邮箱"><el-input v-model="form.email" /></el-form-item>
      <el-form-item label="排序"><el-input-number v-model="form.sort" /></el-form-item>
      <el-form-item label="启用"><el-switch v-model="form.isEnabled" /></el-form-item>
    </el-form>
    <template #footer>
      <el-button @click="dialog = false">取消</el-button>
      <el-button type="primary" @click="save">保存</el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import LemonPageHeader from '@/components/lemon/LemonPageHeader.vue'
import LemonTable from '@/components/lemon/LemonTable.vue'
import { createDepartment, deleteDepartment, fetchDepartmentManagerOptions, fetchDepartments, updateDepartment } from '@/api/system'
import { useAuthStore } from '@/stores/auth'
import type { AdminOption, Department } from '@/types/api'
import type { LemonColumn } from '@/types/table'

const auth = useAuthStore()
const rows = ref<Department[]>([])
const managerOptions = ref<AdminOption[]>([])
const dialog = ref(false)
const editing = ref<number>()
const canWrite = computed(() =>
  auth.hasPermission('system.department.update') || auth.hasPermission('system.department.delete'))
const parentOptions = computed(() => rows.value.filter(item => item.id !== editing.value))
const form = reactive({
  parentId: undefined as number | undefined,
  managerAdminId: undefined as number | undefined,
  name: '',
  code: '',
  phone: '',
  email: '',
  sort: 0,
  isEnabled: true
})

const columns: LemonColumn<Department>[] = [
  { key: 'name', label: '部门名称', fixed: 'left' },
  { key: 'code', label: '部门代码' },
  { key: 'managerName', label: '部门主管' },
  { key: 'phone', label: '电话' },
  { key: 'email', label: '邮箱' },
  { key: 'sort', label: '排序' },
  { key: 'isEnabled', label: '状态' }
]

async function load() {
  ;[rows.value, managerOptions.value] = await Promise.all([
    fetchDepartments(),
    fetchDepartmentManagerOptions()
  ])
}

function create() {
  editing.value = undefined
  Object.assign(form, { parentId: undefined, managerAdminId: undefined, name: '', code: '', phone: '', email: '', sort: 0, isEnabled: true })
  dialog.value = true
}

function edit(row: Department) {
  editing.value = row.id
  Object.assign(form, row)
  dialog.value = true
}

async function save() {
  if (editing.value) await updateDepartment(editing.value, form)
  else await createDepartment(form)
  dialog.value = false
  ElMessage.success('保存成功')
  load()
}

async function remove(row: Department) {
  await ElMessageBox.confirm(`删除部门“${row.name}”？`)
  await deleteDepartment(row.id)
  load()
}

async function importRows(items: Record<string, string>[]) {
  const rowsToImport = items.filter(item => item['部门名称']?.trim() || item['部门代码']?.trim())
  const invalid = rowsToImport.findIndex(item => !item['部门名称']?.trim() || !item['部门代码']?.trim())
  if (invalid >= 0) {
    ElMessage.error(`第 ${invalid + 2} 行缺少部门名称或部门代码`)
    return
  }

  for (const item of rowsToImport) {
    const managerAdminId = Number(item['主管管理员ID'])
    await createDepartment({
      name: item['部门名称'].trim(),
      code: item['部门代码'].trim(),
      managerAdminId: Number.isSafeInteger(managerAdminId) && managerAdminId > 0 ? managerAdminId : undefined,
      phone: item['电话']?.trim(),
      email: item['邮箱']?.trim(),
      sort: Number(item['排序']) || 0,
      isEnabled: item['状态'] !== '禁用'
    })
  }
  ElMessage.success(`导入 ${rowsToImport.length} 条`)
  load()
}

onMounted(load)
</script>
