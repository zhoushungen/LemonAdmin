<template>
  <div class="shell">
    <aside :class="['sidebar', { collapsed: ui.collapsed }]">
      <div class="brand"><div class="logo">L</div><span v-if="!ui.collapsed">Lemon</span></div>
      <el-menu router :default-active="route.path" :collapse="ui.collapsed">
        <el-menu-item v-if="auth.hasPermission('system.dashboard.read')" index="/dashboard">
          <el-icon><HomeFilled /></el-icon><template #title>工作台</template>
        </el-menu-item>
        <el-sub-menu index="system">
          <template #title><el-icon><Setting /></el-icon><span>系统管理</span></template>
          <el-menu-item v-if="auth.hasPermission('system.admin.read')" index="/system/admins">管理员管理</el-menu-item>
          <el-menu-item v-if="auth.hasPermission('system.department.read')" index="/system/departments">部门管理</el-menu-item>
          <el-menu-item v-if="auth.hasPermission('system.role.read')" index="/system/roles">角色权限</el-menu-item>
          <el-menu-item v-if="auth.hasPermission('system.menu.read')" index="/system/menus">菜单管理</el-menu-item>
          <el-menu-item v-if="auth.hasPermission('system.setting.read')" index="/system/settings">系统设置</el-menu-item>
          <el-menu-item v-if="auth.hasPermission('system.audit.read')" index="/system/audit-logs">审计日志</el-menu-item>
        </el-sub-menu>
      </el-menu>
    </aside>

    <main class="main">
      <div v-if="auth.isImpersonating" class="impersonation-banner">
        <span>正在以“{{ auth.state?.displayName }}”身份操作，真实账号：{{ auth.state?.originalDisplayName }}</span>
        <el-button size="small" type="warning" plain :loading="switchingBack" @click="switchBack">切回超级管理员</el-button>
      </div>
      <header class="topbar">
        <el-button text @click="ui.collapsed = !ui.collapsed"><el-icon size="20"><Fold v-if="!ui.collapsed" /><Expand v-else /></el-icon></el-button>
        <div class="top-actions">
          <el-button v-if="auth.features.themeSwitchEnabled || auth.features.fontSizeSwitchEnabled" text @click="themeOpen = true">
            <el-icon><Brush /></el-icon>字号与配色
          </el-button>
          <el-dropdown>
            <span>{{ auth.state?.displayName || '管理员' }} ▾</span>
            <template #dropdown><el-dropdown-menu><el-dropdown-item @click="logout">退出登录</el-dropdown-item></el-dropdown-menu></template>
          </el-dropdown>
        </div>
      </header>
      <section class="content"><router-view /></section>
    </main>
    <LemonThemeDrawer v-model="themeOpen" />
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import { ElMessage } from 'element-plus'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useUiStore } from '@/stores/ui'
import LemonThemeDrawer from '@/components/lemon/LemonThemeDrawer.vue'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const ui = useUiStore()
const themeOpen = ref(false)
const switchingBack = ref(false)

watch(() => auth.features, value => ui.applyFeatureFlags(value), { immediate: true, deep: true })

async function switchBack() {
  switchingBack.value = true
  try {
    await auth.stopImpersonating()
    ElMessage.success('已切回超级管理员')
    await router.replace('/dashboard')
  } finally {
    switchingBack.value = false
  }
}

async function logout() {
  await auth.logout()
  await router.push('/login')
}
</script>

<style scoped>
.shell { display: flex; min-height: 100vh }
.sidebar { width: 230px; background: white; border-right: 1px solid #e5e7eb; transition: .2s; position: fixed; inset: 0 auto 0 0; z-index: 10 }
.sidebar.collapsed { width: 64px }
.brand { height: 60px; display: flex; align-items: center; gap: 10px; padding: 0 16px; font-size: 20px; font-weight: 800; color: var(--lemon-primary) }
.logo { width: 34px; height: 34px; border-radius: 10px; background: var(--lemon-primary); color: white; display: grid; place-items: center }
.el-menu { border-right: none }
.main { margin-left: 230px; min-width: 0; flex: 1; transition: .2s }
.sidebar.collapsed + .main { margin-left: 64px }
.impersonation-banner { min-height: 42px; padding: 6px 20px; display: flex; align-items: center; justify-content: center; gap: 16px; background: #fff7ed; color: #9a3412; border-bottom: 1px solid #fed7aa; position: sticky; top: 0; z-index: 6 }
.topbar { height: 60px; background: white; border-bottom: 1px solid #e5e7eb; display: flex; align-items: center; justify-content: space-between; padding: 0 20px; position: sticky; top: 0; z-index: 5 }
.impersonation-banner + .topbar { top: 42px }
.top-actions { display: flex; align-items: center; gap: 16px }
.content { padding: 20px; max-width: 1800px; margin: auto }
</style>
