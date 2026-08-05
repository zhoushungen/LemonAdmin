<template>
  <LemonPageHeader title="菜单管理" description="菜单展示与接口权限分离，permissionCode 决定可见性">
    <el-button v-if="auth.hasPermission('system.menu.create')" type="primary" @click="create">新增菜单</el-button>
  </LemonPageHeader>

  <div class="page-card">
    <LemonTable table-key="system-menus" export-name="菜单" :rows="rows" :columns="columns" @refresh="load">
      <template #cell-isVisible="{ row }"><el-tag :type="row.isVisible ? 'success' : 'info'">{{ row.isVisible ? '显示' : '隐藏' }}</el-tag></template>
      <template #cell-isEnabled="{ row }"><el-tag :type="row.isEnabled ? 'success' : 'info'">{{ row.isEnabled ? '启用' : '禁用' }}</el-tag></template>
      <template v-if="canWrite" #actions>
        <el-table-column fixed="right" label="操作" width="150">
          <template #default="{ row }">
            <el-button v-if="auth.hasPermission('system.menu.update')" link type="primary" @click="edit(row)">编辑</el-button>
            <el-button v-if="auth.hasPermission('system.menu.delete')" link type="danger" @click="remove(row)">删除</el-button>
          </template>
        </el-table-column>
      </template>
    </LemonTable>
  </div>

  <el-dialog v-model="dialog" :title="editing ? '编辑菜单' : '新增菜单'" width="600px">
    <el-form label-width="100px">
      <el-form-item label="上级菜单">
        <el-select v-model="form.parentId" clearable class="w-full">
          <el-option v-for="menu in rows.filter(item => item.id !== editing && item.menuType !== 'button')" :key="menu.id" :label="menu.name" :value="menu.id" />
        </el-select>
      </el-form-item>
      <el-form-item label="名称"><el-input v-model="form.name" /></el-form-item>
      <el-form-item label="类型">
        <el-radio-group v-model="form.menuType">
          <el-radio-button value="directory">目录</el-radio-button>
          <el-radio-button value="page">页面</el-radio-button>
          <el-radio-button value="button">按钮</el-radio-button>
        </el-radio-group>
      </el-form-item>
      <el-form-item label="路由路径"><el-input v-model="form.routePath" /></el-form-item>
      <el-form-item label="组件"><el-input v-model="form.component" /></el-form-item>
      <el-form-item label="图标"><el-input v-model="form.icon" /></el-form-item>
      <el-form-item label="权限码"><el-input v-model="form.permissionCode" /></el-form-item>
      <el-form-item label="排序"><el-input-number v-model="form.sort" /></el-form-item>
      <el-form-item label="显示 / 启用">
        <el-switch v-model="form.isVisible" active-text="显示" />
        <el-switch v-model="form.isEnabled" active-text="启用" style="margin-left: 20px" />
      </el-form-item>
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
import { createMenu, deleteMenu, fetchMenus, updateMenu } from '@/api/system'
import { useAuthStore } from '@/stores/auth'
import type { MenuItem } from '@/types/api'
import type { LemonColumn } from '@/types/table'

const auth = useAuthStore()
const rows = ref<MenuItem[]>([])
const dialog = ref(false)
const editing = ref<number>()
const canWrite = computed(() => auth.hasPermission('system.menu.update') || auth.hasPermission('system.menu.delete'))
const form = reactive<any>({ parentId: undefined, name: '', menuType: 'page', routeName: '', routePath: '', component: '', icon: '', permissionCode: '', sort: 0, isVisible: true, isEnabled: true })
const columns: LemonColumn<MenuItem>[] = [
  { key: 'name', label: '菜单名称', fixed: 'left' },
  { key: 'menuType', label: '类型' },
  { key: 'routePath', label: '路由' },
  { key: 'component', label: '组件', minWidth: 220 },
  { key: 'permissionCode', label: '权限码', minWidth: 200 },
  { key: 'sort', label: '排序' },
  { key: 'isVisible', label: '可见' },
  { key: 'isEnabled', label: '状态' }
]

async function load() { rows.value = await fetchMenus() }
function create() {
  editing.value = undefined
  Object.assign(form, { parentId: undefined, name: '', menuType: 'page', routeName: '', routePath: '', component: '', icon: '', permissionCode: '', sort: 0, isVisible: true, isEnabled: true })
  dialog.value = true
}
function edit(row: MenuItem) { editing.value = row.id; Object.assign(form, row); dialog.value = true }
async function save() {
  if (editing.value) await updateMenu(editing.value, form)
  else await createMenu(form)
  dialog.value = false
  ElMessage.success('保存成功')
  load()
}
async function remove(row: MenuItem) {
  await ElMessageBox.confirm(`删除菜单“${row.name}”？`)
  await deleteMenu(row.id)
  load()
}

onMounted(load)
</script>
