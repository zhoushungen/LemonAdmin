import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const routes: RouteRecordRaw[] = [
  {
    path: '/login',
    component: () => import('@/views/login/index.vue'),
    meta: { public: true }
  },
  {
    path: '/',
    component: () => import('@/layouts/AdminLayout.vue'),
    redirect: '/dashboard',
    children: [
      { path: 'dashboard', component: () => import('@/views/dashboard/index.vue'), meta: { title: '工作台', permission: 'system.dashboard.read' } },
      { path: 'system/admins', component: () => import('@/views/system/admins/index.vue'), meta: { title: '管理员管理', permission: 'system.admin.read' } },
      { path: 'system/departments', component: () => import('@/views/system/departments/index.vue'), meta: { title: '部门管理', permission: 'system.department.read' } },
      { path: 'system/roles', component: () => import('@/views/system/roles/index.vue'), meta: { title: '角色权限', permission: 'system.role.read' } },
      { path: 'system/menus', component: () => import('@/views/system/menus/index.vue'), meta: { title: '菜单管理', permission: 'system.menu.read' } },
      { path: 'system/settings', component: () => import('@/views/system/settings/index.vue'), meta: { title: '系统设置', permission: 'system.setting.read' } },
      { path: 'system/audit-logs', component: () => import('@/views/system/audit-logs/index.vue'), meta: { title: '审计日志', permission: 'system.audit.read' } }
    ]
  },
  { path: '/:pathMatch(.*)*', component: () => import('@/views/not-found.vue') }
]

const router = createRouter({ history: createWebHistory(), routes })

router.beforeEach(to => {
  const auth = useAuthStore()
  if (!to.meta.public && !auth.isLoggedIn) return '/login'
  if (to.path === '/login' && auth.isLoggedIn) return '/dashboard'

  const permission = to.meta.permission as string | undefined
  if (permission && !auth.hasPermission(permission)) return '/dashboard'
  return true
})

export default router
