import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { loginApi, logoutAllApi, startImpersonation, stopImpersonation } from '@/api/system'
import type { AuthResponse, SystemFeatureFlags } from '@/types/api'
import { readJson, writeJson } from '@/utils/storage'

const STORAGE_KEY = 'lemon.auth'
const ACCESS_TOKEN_KEY = 'lemon.accessToken'
const defaultFeatures: SystemFeatureFlags = {
  accountSwitchEnabled: false,
  themeSwitchEnabled: true,
  fontSizeSwitchEnabled: true
}

export const useAuthStore = defineStore('auth', () => {
  const state = ref<AuthResponse | null>(readJson<AuthResponse | null>(STORAGE_KEY, null))
  const isLoggedIn = computed(() => Boolean(state.value?.accessToken))
  const permissions = computed(() => state.value?.permissions ?? [])
  const isSuperAdmin = computed(() => state.value?.isSuperAdmin === true)
  const isImpersonating = computed(() => state.value?.isImpersonating === true)
  const features = computed(() => state.value?.features ?? defaultFeatures)

  const hasPermission = (code?: string) =>
    !code || permissions.value.includes('*') || permissions.value.includes(code)

  async function login(username: string, password: string) {
    setState(await loginApi({ username, password }))
  }

  async function impersonate(targetAdminId: number, reason: string) {
    setState(await startImpersonation(targetAdminId, reason))
  }

  async function stopImpersonating() {
    setState(await stopImpersonation())
  }

  function updateFeatures(next: SystemFeatureFlags) {
    if (!state.value) return
    state.value = { ...state.value, features: next }
    persist()
  }

  function setState(next: AuthResponse) {
    state.value = next
    persist()
  }

  function persist() {
    if (!state.value) return
    writeJson(STORAGE_KEY, state.value)
    localStorage.setItem(ACCESS_TOKEN_KEY, state.value.accessToken)
  }

  async function logout() {
    try {
      if (state.value?.accessToken) await logoutAllApi()
    } finally {
      state.value = null
      localStorage.removeItem(STORAGE_KEY)
      localStorage.removeItem(ACCESS_TOKEN_KEY)
    }
  }

  return {
    state,
    isLoggedIn,
    permissions,
    isSuperAdmin,
    isImpersonating,
    features,
    hasPermission,
    login,
    impersonate,
    stopImpersonating,
    updateFeatures,
    logout
  }
})
