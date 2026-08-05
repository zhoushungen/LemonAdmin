export interface LemonColumn<T = Record<string, unknown>> {
  key: string
  label: string
  width?: number | string
  minWidth?: number
  fixed?: 'left' | 'right'
  visible?: boolean
  sortable?: boolean
  formatter?: (row: T) => string | number | undefined
  exportable?: boolean
}
