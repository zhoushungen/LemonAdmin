import { computed, ref, watch, type Ref } from 'vue'
import type { LemonColumn } from '@/types/table'
import { readJson, writeJson } from '@/utils/storage'

type Preference = { visible: boolean; order: number }

export function useTablePreferences<T>(tableKey: string, source: Ref<LemonColumn<T>[]>) {
  const storageKey = `lemon.table.${tableKey}`
  const preferences = ref<Record<string, Preference>>(readJson(storageKey, {}))

  const columns = computed(() =>
    source.value
      .map((column, index) => ({
        ...column,
        visible: preferences.value[column.key]?.visible ?? column.visible ?? true,
        _order: preferences.value[column.key]?.order ?? index
      }))
      .sort((left, right) => left._order - right._order)
  )

  function setColumns(next: LemonColumn<T>[]) {
    preferences.value = Object.fromEntries(
      next.map((column, index) => [column.key, { visible: column.visible !== false, order: index }])
    )
  }

  function reset() {
    preferences.value = {}
  }

  watch(preferences, value => writeJson(storageKey, value), { deep: true })
  return { columns, setColumns, reset }
}
