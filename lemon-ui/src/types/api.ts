export interface ApiResponse<T = unknown> { code: string; message: string; data: T; traceId?: string }
export interface PagedResult<T> { items: T[]; pageIndex: number; pageSize: number; total: number }

export interface SystemFeatureFlags {
  accountSwitchEnabled: boolean
  themeSwitchEnabled: boolean
  fontSizeSwitchEnabled: boolean
}

export interface AuthResponse {
  userId: number
  departmentId?: number
  roleId?: number
  username: string
  displayName: string
  isSuperAdmin: boolean
  isImpersonating: boolean
  originalUserId?: number
  originalUsername?: string
  originalDisplayName?: string
  accessToken: string
  accessTokenExpiresAt: string
  refreshToken: string
  refreshTokenExpiresAt: string
  permissions: string[]
  features: SystemFeatureFlags
}

export interface Admin {
  id: number
  departmentId?: number
  departmentName?: string
  roleId?: number
  roleName?: string
  username: string
  displayName: string
  email?: string
  mobile?: string
  isSuperAdmin: boolean
  isEnabled: boolean
  lastLoginAt?: string
  lastLoginIp?: string
  createdAt: string
}

export interface AdminDetail {
  id: number
  departmentId?: number
  roleId?: number
  username: string
  displayName: string
  email?: string
  mobile?: string
  isSuperAdmin: boolean
  isEnabled: boolean
}

export interface AdminOption {
  id: number
  displayName: string
  username: string
  departmentId?: number
}

export interface Department {
  id: number
  parentId?: number
  managerAdminId?: number
  managerName?: string
  name: string
  code: string
  phone?: string
  email?: string
  sort: number
  isEnabled: boolean
}

export enum DataScopeType {
  All = 1,
  DepartmentAndChildren = 2,
  Department = 3,
  ManagedDepartments = 4,
  Self = 5
}

export interface Role {
  id: number
  code: string
  name: string
  description?: string
  dataScope: DataScopeType
  isSystem: boolean
  isEnabled: boolean
  permissionIds: number[]
}

export interface Permission { id: number; code: string; name: string; module: string }
export interface MenuItem { id: number; parentId?: number; name: string; menuType: string; routeName?: string; routePath?: string; component?: string; icon?: string; permissionCode?: string; sort: number; isVisible: boolean; isEnabled: boolean }
export interface Setting { id: number; settingGroup: string; settingKey: string; settingValue: string; valueType: string; description?: string; isPublic: boolean }
export interface AuditLog { id: number; adminUserId?: number; departmentId?: number; actorAdminUserId?: number; isImpersonating: boolean; module: string; action: string; requestPath: string; httpMethod: string; statusCode: number; ipAddress?: string; elapsedMilliseconds: number; traceId?: string; createdAt: string }
