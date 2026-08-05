import { defineStore } from 'pinia'
import { ref, watch } from 'vue'
import type { SystemFeatureFlags } from '@/types/api'

type FontSize = 'small' | 'medium' | 'large'
type Theme = 'blue' | 'green' | 'purple' | 'orange' | 'dark'

const colors: Record<Theme, string> = {
  blue: '#3b82f6',
  green: '#10b981',
  purple: '#8b5cf6',
  orange: '#f97316',
  dark: '#334155'
}
const sizes: Record<FontSize, string> = {
  small: '13px',
  medium: '14px',
  large: '16px'
}

function readFontSize(): FontSize {
  const value = localStorage.getItem('lemon.fontSize')
  return value === 'small' || value === 'large' ? value : 'medium'
}

function readTheme(): Theme {
  const value = localStorage.getItem('lemon.theme')
  return value && value in colors ? (value as Theme) : 'blue'
}

export const useUiStore = defineStore('ui', () => {
  const fontSize = ref<FontSize>(readFontSize())
  const theme = ref<Theme>(readTheme())
  const collapsed = ref(false)
  const flags = ref<SystemFeatureFlags>({
    accountSwitchEnabled: false,
    themeSwitchEnabled: true,
    fontSizeSwitchEnabled: true
  })

  function applyFeatureFlags(next: SystemFeatureFlags) {
    flags.value = next
    if (!next.fontSizeSwitchEnabled) fontSize.value = 'medium'
    if (!next.themeSwitchEnabled) theme.value = 'blue'
    apply()
  }

  function apply() {
    const root = document.documentElement
    root.style.setProperty('--lemon-font-size', sizes[fontSize.value])
    root.style.setProperty('--lemon-primary', colors[theme.value])
    root.style.setProperty('--el-color-primary', colors[theme.value])
    root.style.setProperty('--el-font-size-base', sizes[fontSize.value])
    root.dataset.theme = theme.value
    localStorage.setItem('lemon.fontSize', fontSize.value)
    localStorage.setItem('lemon.theme', theme.value)
  }

  watch([fontSize, theme], apply, { immediate: true })
  return { fontSize, theme, collapsed, flags, colors, apply, applyFeatureFlags }
})
