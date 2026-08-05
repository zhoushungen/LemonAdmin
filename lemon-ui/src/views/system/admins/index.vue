<template>
  <LemonPageHeader title="管理员管理" description="普通管理员只绑定一个角色；角色为空的账号为超级管理员">
    <el-button v-if="auth.isSuperAdmin" type="primary" @click="openCreate">新增管理员</el-button>
  </LemonPageHeader>

  <LemonSearchForm @search="load" @reset="reset">
    <el-input v-model="query.keyword" placeholder="用户名 / 姓名 / 手机" clearable style="width: 220px" />
    <el-select v-model="query.departmentId" placeholder="全部部门" clearable style="width: 180px">
      <el-option v-for="department in departments" :key="department.id" :label="department.name" :value="department.id" />
    </el-select>
    <el-select v-model="query.enabled" placeholder="全部状态" clearable style="width: 140px">
      <el-option label="启用" :value="true" /><el-option label="禁用" :value="false" />
    </el-select>
  </LemonSearchForm>

  <div class="page-card">
    <LemonTable
      table-key="system-admins"
      export-name="管理员"
      :rows="rows"
      :columns="columns"
      :loading="loading"
      :importable="auth.isSuperAdmin"
      :pagination="page"
      @refresh="load"
      @page-change="changePage"
      @import="importRows"
    >
      <template #cell-isEnabled="{ row }"><el-tag :type="row.isEnabled ? 'success' : 'info'">{{ row.isEnabled ? '启用' : '禁用' }}</el-tag></template>
      <template #cell-isSuperAdmin="{ row }"><el-tag v-if="row.isSuperAdmin" type="danger">超级管理员</el-tag><span v-else>普通管理员</span></template>
      <template #actions>
        <el-table-column fixed="right" label="操作" width="190">
          <template #default="{ row }">
            <el-button v-if="auth.isSuperAdmin" link type="primary" @click="edit(row)">编辑</el-button>
            <el-button
              v-if="canSwitch(row)"
              link
              type="warning"
              @click="openSwitch(row)"
            >切换账号</el-button>
          </template>
        </el-table-column>
      </template>
    </LemonTable>
  </div>

  <el-dialog v-model="dialog" :title="editing ? '编辑管理员' : '新增管理员'" width="560px">
    <el-form label-width="90px">
      <el-form-item label="用户名"><el-input v-model="form.username" :disabled="!!editing" /></el-form-item>
      <el-form-item v-if="!editing" label="密码"><el-input v-model="form.password" type="password" show-password /></el-form-item>
      <el-form-item label="姓名"><el-input v-model="form.displayName" /></el-form-item>
      <el-form-item label="部门">
        <el-select v-model="form.departmentId" clearable class="w-full">
          <el-option v-for="department in departments" :key="department.id" :label="department.name" :value="department.id" />
        </el-select>
      </el-form-item>
      <el-form-item label="角色">
        <el-select v-model="form.roleId" :disabled="editingSuperAdmin" class="w-full" placeholder="请选择一个角色">
          <el-option v-for="role in enabledRoles" :key="role.id" :label="role.name" :value="role.id" />
        </el-select>
        <div v-if="editingSuperAdmin" class="text-muted">超级管理员的角色固定为空。</div>
      </el-form-item>
      <el-form-item label="手机"><el-input v-model="form.mobile" /></el-form-item>
      <el-form-item label="邮箱"><el-input v-model="form.email" /></el-form-item>
      <el-form-item v-if="editing" label="状态"><el-switch v-model="form.isEnabled" :disabled="editingSuperAdmin" /></el-form-item>
    </el-form>
    <template #footer><el-button @click="dialog = false">取消</el-button><el-button type="primary" @click="save">保存</el-button></template>
  </el-dialog>

  <el-dialog v-model="switchDialog" title="切换后台账号" width="480px">
    <el-alert type="warning" :closable="false" show-icon>
      切换后将完全使用“{{ switchingTarget?.displayName }}”的菜单与权限，所有操作会记录真实超级管理员。
    </el-alert>
    <el-form label-width="80px" style="margin-top: 18px">
      <el-form-item label="切换原因"><el-input v-model="switchReason" type="textarea" :rows="3" maxlength="200" show-word-limit /></el-form-item>
    </el-form>
    <template #footer><el-button @click="switchDialog = false">取消</el-button><el-button type="warning" :loading="switching" @click="confirmSwitch">确认切换</el-button></template>
  </el-dialog>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { useRouter } from 'vue-router'
import LemonPageHeader from '@/components/lemon/LemonPageHeader.vue'
import LemonSearchForm from '@/components/lemon/LemonSearchForm.vue'
import LemonTable from '@/components/lemon/LemonTable.vue'
import { createAdmin, fetchAdmin, fetchAdmins, fetchDepartments, fetchRoles, updateAdmin } from '@/api/system'
import { useAuthStore } from '@/stores/auth'
import type { Admin, Department, Role } from '@/types/api'
import type { LemonColumn } from '@/types/table'

const auth = useAuthStore()
const router = useRouter()
const rows = ref<Admin[]>([])
const departments = ref<Department[]>([])
const roles = ref<Role[]>([])
const loading = ref(false)
const dialog = ref(false)
const editing = ref<number>()
const editingSuperAdmin = ref(false)
const switchDialog = ref(false)
const switchingTarget = ref<Admin>()
const switchReason = ref('')
const switching = ref(false)
const query = reactive<{ keyword: string; departmentId?: number; enabled?: boolean }>({ keyword: '' })
const page = reactive({ pageIndex: 1, pageSize: 20, total: 0 })
const form = reactive<any>({ username: '', password: '', displayName: '', departmentId: undefined, roleId: undefined, mobile: '', email: '', isEnabled: true })
const enabledRoles = computed(() => roles.value.filter(role => role.isEnabled))

const columns: LemonColumn<Admin>[] = [
  { key: 'username', label: '用户名', fixed: 'left' },
  { key: 'displayName', label: '姓名' },
  { key: 'departmentName', label: '部门' },
  { key: 'roleName', label: '角色', minWidth: 150 },
  { key: 'isSuperAdmin', label: '权限级别' },
  { key: 'mobile', label: '手机' },
  { key: 'email', label: '邮箱', minWidth: 180 },
  { key: 'isEnabled', label: '状态' },
  { key: 'lastLoginAt', label: '最后登录', minWidth: 170 },
  { key: 'lastLoginIp', label: '登录 IP' },
  { key: 'createdAt', label: '创建时间', minWidth: 170 }
]

async function load() {
  loading.value = true
  try {
    const result = await fetchAdmins({ ...query, pageIndex: page.pageIndex, pageSize: page.pageSize })
    rows.value = result.items
    page.total = result.total
  } finally {
    loading.value = false
  }
}

function reset() {
  query.keyword = ''
  query.departmentId = undefined
  query.enabled = undefined
  page.pageIndex = 1
  load()
}
function changePage(value: any) { page.pageIndex = value.pageIndex; page.pageSize = value.pageSize; load() }
function openCreate() {
  editing.value = undefined
  editingSuperAdmin.value = false
  Object.assign(form, { username: '', password: '', displayName: '', departmentId: undefined, roleId: undefined, mobile: '', email: '', isEnabled: true })
  dialog.value = true
}
async function edit(row: Admin) {
  editing.value = row.id
  const detail = await fetchAdmin(row.id)
  editingSuperAdmin.value = detail.isSuperAdmin
  Object.assign(form, detail)
  dialog.value = true
}
async function save() {
  if (!editingSuperAdmin.value && !form.departmentId) {
    ElMessage.warning('请选择主部门')
    return
  }
  if (!editingSuperAdmin.value && !form.roleId) {
    ElMessage.warning('请选择角色')
    return
  }
  if (editing.value) await updateAdmin(editing.value, form)
  else await createAdmin(form)
  dialog.value = false
  ElMessage.success('保存成功')
  load()
}
function canSwitch(row: Admin) {
  return auth.isSuperAdmin && auth.features.accountSwitchEnabled && row.isEnabled && !row.isSuperAdmin && row.id !== auth.state?.userId
}
function openSwitch(row: Admin) {
  switchingTarget.value = row
  switchReason.value = ''
  switchDialog.value = true
}
async function confirmSwitch() {
  if (!switchingTarget.value || switchReason.value.trim().length < 2) {
    ElMessage.warning('请填写切换原因')
    return
  }
  switching.value = true
  try {
    await auth.impersonate(switchingTarget.value.id, switchReason.value.trim())
    switchDialog.value = false
    ElMessage.success(`已切换为 ${switchingTarget.value.displayName}`)
    await router.replace('/dashboard')
  } finally {
    switching.value = false
  }
}
async function importRows(items: Record<string, string>[]) {
  const rows = items.filter(item => Object.values(item).some(value => String(value ?? '').trim()))
  const errors: string[] = []

  const payloads = rows.map((item, index) => {
    const username = String(item['用户名'] ?? '').trim()
    const displayName = String(item['姓名'] ?? '').trim()
    const password = String(item['密码'] ?? '')
    const roleId = Number(item['角色ID'])
    const departmentId = Number(item['部门ID'])

    if (!username) errors.push(`第 ${index + 2} 行缺少用户名`)
    if (!displayName) errors.push(`第 ${index + 2} 行缺少姓名`)
    if (password.length < 8) errors.push(`第 ${index + 2} 行密码至少 8 位`)
    if (!Number.isSafeInteger(roleId) || roleId <= 0) errors.push(`第 ${index + 2} 行角色ID无效`)
    if (item['部门ID'] && (!Number.isSafeInteger(departmentId) || departmentId <= 0))
      errors.push(`第 ${index + 2} 行部门ID无效`)

    return {
      username,
      displayName,
      password,
      departmentId: Number.isSafeInteger(departmentId) && departmentId > 0 ? departmentId : undefined,
      roleId: Number.isSafeInteger(roleId) && roleId > 0 ? roleId : undefined,
      mobile: String(item['手机'] ?? '').trim() || undefined,
      email: String(item['邮箱'] ?? '').trim() || undefined
    }
  })

  if (!rows.length) {
    ElMessage.warning('没有可导入的数据')
    return
  }
  if (errors.length) {
    ElMessage.error(errors.slice(0, 5).join('；') + (errors.length > 5 ? `；另有 ${errors.length - 5} 项` : ''))
    return
  }

  for (const payload of payloads) await createAdmin(payload)
  ElMessage.success(`导入 ${payloads.length} 条`)
  load()
}

onMounted(async () => {
  ;[departments.value, roles.value] = await Promise.all([fetchDepartments(), fetchRoles()])
  load()
})
</script>
