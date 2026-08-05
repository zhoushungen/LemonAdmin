export function escapeCsv(value: unknown): string {
  let text = value == null ? '' : String(value)
  if (typeof value === 'string' && /^[\t\r ]*[=+@-]/.test(text)) text = `'${text}`
  return /[",\r\n]/.test(text) ? `"${text.replaceAll('"', '""')}"` : text
}

export function downloadCsv(filename: string, headers: string[], rows: unknown[][]): void {
  const content = `\uFEFF${[headers, ...rows]
    .map((row) => row.map(escapeCsv).join(','))
    .join('\r\n')}`
  const url = URL.createObjectURL(new Blob([content], { type: 'text/csv;charset=utf-8' }))
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = filename
  anchor.click()
  URL.revokeObjectURL(url)
}

export function parseCsv(text: string): string[][] {
  const rows: string[][] = []
  let row: string[] = []
  let cell = ''
  let quoted = false

  for (let index = 0; index < text.length; index += 1) {
    const char = text[index]
    if (quoted) {
      if (char === '"' && text[index + 1] === '"') {
        cell += '"'
        index += 1
      } else if (char === '"') {
        quoted = false
      } else {
        cell += char
      }
    } else if (char === '"') {
      quoted = true
    } else if (char === ',') {
      row.push(cell)
      cell = ''
    } else if (char === '\n') {
      row.push(cell.replace(/\r$/, ''))
      rows.push(row)
      row = []
      cell = ''
    } else {
      cell += char
    }
  }

  if (cell || row.length) {
    row.push(cell)
    rows.push(row)
  }

  return rows.filter((item) => item.some(Boolean))
}
