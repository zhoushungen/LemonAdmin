import http from './http'
import type { Admin, AdminDetail, AdminOption, AuditLog, AuthResponse, Department, MenuItem, PagedResult, Permission, Role, Setting } from '@/types/api'

export const loginApi = (data: { username: string; password: string }) => http.post<never, AuthResponse>('/auth/login', data)
export const logoutAllApi = () => http.post('/auth/logout-all')
export const fetchAdmins = (params: Record<string, unknown>) => http.get<never, PagedResult<Admin>>('/admins', { params })
export const fetchAdmin = (id: number) => http.get<never, AdminDetail>(`/admins/${id}`)
export const createAdmin = (data: Record<string, unknown>) => http.post('/admins', data)
export const updateAdmin = (id: number, data: Record<string, unknown>) => http.put(`/admins/${id}`, data)
export const startImpersonation = (targetAdminId: number, reason: string) => http.post<never, AuthResponse>('/impersonation/start', { targetAdminId, reason })
export const stopImpersonation = () => http.post<never, AuthResponse>('/impersonation/stop')

export const fetchDepartments = () => http.get<never, Department[]>('/departments')
export const fetchDepartmentManagerOptions = () => http.get<never, AdminOption[]>('/departments/manager-options')
export const createDepartment = (data: Record<string, unknown>) => http.post('/departments', data)
export const updateDepartment = (id: number, data: Record<string, unknown>) => http.put(`/departments/${id}`, data)
export const deleteDepartment = (id: number) => http.delete(`/departments/${id}`)

export const fetchRoles = () => http.get<never, Role[]>('/roles')
export const fetchPermissions = () => http.get<never, Permission[]>('/roles/permissions')
export const createRole = (data: Record<string, unknown>) => http.post('/roles', data)
export const updateRole = (id: number, data: Record<string, unknown>) => http.put(`/roles/${id}`, data)
export const updateRolePermissions = (id: number, permissionIds: number[]) => http.put(`/roles/${id}/permissions`, { permissionIds })

export const fetchMenus = () => http.get<never, MenuItem[]>('/menus')
export const fetchCurrentMenus = () => http.get<never, MenuItem[]>('/menus/current')
export const createMenu = (data: Record<string, unknown>) => http.post('/menus', data)
export const updateMenu = (id: number, data: Record<string, unknown>) => http.put(`/menus/${id}`, data)
export const deleteMenu = (id: number) => http.delete(`/menus/${id}`)

export const fetchSettings = (group?: string) => http.get<never, Setting[]>('/settings', { params: { group } })
export const updateSetting = (key: string, data: Record<string, unknown>) => http.put(`/settings/${encodeURIComponent(key)}`, data)
export const fetchAuditLogs = (params: Record<string, unknown>) => http.get<never, PagedResult<AuditLog>>('/audit-logs', { params })
